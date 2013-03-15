using System;
using Microsoft.SPOT;
using System.Collections;
using Microsoft.SPOT.Hardware;
using System.IO.Ports;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus
{
    #region Enums, delegales and event args

    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; private set; }

        public bool Cancel { get; set; }

        public MessageEventArgs(Message message)
        {
            Message = message;
        }
    }

    public delegate void MessageEventHandler(MessageEventArgs e);

    #endregion


    public static class Manager
    {
        static SerialInterruptPort iBus;

        public static void Init(String port, Cpu.Pin busy)
        {
            iBus = new SerialInterruptPort(new SerialPortConfiguration(port, 9600, Parity.Even, 8, StopBits.One), busy, 0, 1);
            iBus.DataReceived += new SerialDataReceivedEventHandler(iBus_DataReceived);

            messageWriteQueue = new QueueThreadWorker(SendMessage);
            //messageReadQueue = new QueueThreadWorker(ProcessMessage);
        }

        #region Message reading and processing

        static QueueThreadWorker messageReadQueue;

        static byte[] messageBuffer = new byte[Message.PacketLengthMax];
        static byte messageBufferLength = 0;

        static void iBus_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte b = iBus.ReadAvailable()[0];
            if (messageBufferLength >= Message.PacketLengthMax)
            {
                Logger.Warning("Buffer overflow. We can't reach it, yeah?");
                SkipBuffer(1);
            }
            messageBuffer[messageBufferLength++] = b;
            while (messageBufferLength >= Message.PacketLengthMin)
            {
                Message m = Message.TryCreate(messageBuffer, messageBufferLength);
                if (m == null)
                {
                    if (!Message.CanStartWith(messageBuffer, messageBufferLength))
                    {
                        Logger.Warning("Buffer skip: non-iBus data detected.");
                        SkipBuffer(1);
                        continue;
                    }
                    return;
                }
                ProcessMessage(m);
                //messageReadQueue.Enqueue(m);
                SkipBuffer(m.PacketLength);
            }
        }

        static void SkipBuffer(byte count)
        {
            messageBufferLength -= count;
            if (messageBufferLength > 0)
            {
                Array.Copy(messageBuffer, count, messageBuffer, 0, messageBufferLength);
            }
        }

        public static void ProcessMessage(object o)
        {
            var m = (Message)o;

            MessageEventArgs args = null;
            try
            {
                var e = BeforeMessageReceived;
                if (e != null)
                {
                    args = new MessageEventArgs(m);
                    e(args);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "on before message received " + m.ToPrettyString());
            }

            if (args != null && args.Cancel)
            {
                return;
            }

            foreach (MessageReceiverRegistration receiver in messageReceiverList)
            {
                try
                {
                    switch (receiver.Match)
                    {
                        case MessageReceiverRegistration.MatchType.Source:
                            if (receiver.Source == m.SourceDevice)
                            {
                                receiver.Callback(m);
                            }
                            break;
                        case MessageReceiverRegistration.MatchType.Destination:
                            if (receiver.Destination == m.DestinationDevice
                                || m.DestinationDevice == DeviceAddress.Broadcast
                                || m.DestinationDevice == DeviceAddress.GlobalBroadcastAddress)
                            {
                                receiver.Callback(m);
                            }
                            break;
                        case MessageReceiverRegistration.MatchType.SourceAndDestination:
                            if (receiver.Source == m.SourceDevice
                                && (receiver.Destination == m.DestinationDevice
                                    || m.DestinationDevice == DeviceAddress.Broadcast
                                    || m.DestinationDevice == DeviceAddress.GlobalBroadcastAddress))
                            {
                                receiver.Callback(m);
                            }
                            break;
                        case MessageReceiverRegistration.MatchType.SourceOrDestination:
                            if (receiver.Source == m.SourceDevice
                                || receiver.Destination == m.DestinationDevice
                                || m.DestinationDevice == DeviceAddress.Broadcast
                                || m.DestinationDevice == DeviceAddress.GlobalBroadcastAddress)
                            {
                                receiver.Callback(m);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "while processing message: " + m.ToPrettyString());
                }
            }

            try
            {
                var e = AfterMessageReceived;
                if (e != null)
                {
                    if (args == null)
                    {
                        args = new MessageEventArgs(m);
                    }
                    e(args);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "on after message received " + m.ToPrettyString());
            }
        }

        #endregion

        #region Message writing and queue

        static QueueThreadWorker messageWriteQueue;

        static void SendMessage(object m)
        {
            iBus.Write(((Message)m).Packet);

            var e = AfterMessageSent;
            if (e != null)
            {
                e(new MessageEventArgs((Message)m));
            }

            Thread.Sleep(5); // Don't flood iBus
        }

        public static void EnqueueMessage(Message m)
        {
            MessageEventArgs args = null;
            var e = BeforeMessageSent;
            if (e != null)
            {
                args = new MessageEventArgs(m);
                e(args);
            }
            if (args == null || !args.Cancel)
            {
                // TODO benchmark sending in ms
                messageWriteQueue.Enqueue(m);
            }
        }

        #endregion

        #region Message receiver registration

        /// <summary>
        /// Fired before processing the message by registered receivers.
        /// Message processing could be cancelled in this event
        /// </summary>
        public static event MessageEventHandler BeforeMessageReceived;

        /// <summary>
        /// Fired after processing the message by registered receivers
        /// </summary>
        public static event MessageEventHandler AfterMessageReceived;

        /// <summary>
        /// Fired before sending the message.
        /// Message processing could be cancelled in this event
        /// </summary>
        public static event MessageEventHandler BeforeMessageSent;

        /// <summary>
        /// Fired after sending the message
        /// </summary>
        public static event MessageEventHandler AfterMessageSent;

        public delegate void MessageReceiver(Message message);

        class MessageReceiverRegistration
        {
            public enum MatchType
            {
                Source,
                Destination,
                SourceOrDestination,
                SourceAndDestination
            }

            public readonly DeviceAddress Source;
            public readonly DeviceAddress Destination;
            public readonly MessageReceiver Callback;
            public readonly MatchType Match;

            public MessageReceiverRegistration(DeviceAddress source, DeviceAddress destination, MessageReceiver callback, MatchType match)
            {
                Source = source;
                Destination = destination;
                Callback = callback;
                Match = match;
            }
        }

        static ArrayList messageReceiverList = new ArrayList();

        public static void AddMessageReceiverForSourceDevice(DeviceAddress source, MessageReceiver callback)
        {
            messageReceiverList.Add(new MessageReceiverRegistration(source, DeviceAddress.Unset, callback, MessageReceiverRegistration.MatchType.Source));
        }

        public static void AddMessageReceiverForDestinationDevice(DeviceAddress destination, MessageReceiver callback)
        {
            messageReceiverList.Add(new MessageReceiverRegistration(DeviceAddress.Unset, destination, callback, MessageReceiverRegistration.MatchType.Destination));
        }

        public static void AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback)
        {
            messageReceiverList.Add(new MessageReceiverRegistration(source, destination, callback, MessageReceiverRegistration.MatchType.SourceAndDestination));
        }

        public static void AddMessageReceiverForSourceOrDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback)
        {
            messageReceiverList.Add(new MessageReceiverRegistration(source, destination, callback, MessageReceiverRegistration.MatchType.SourceOrDestination));
        }

        #endregion
    }
}
