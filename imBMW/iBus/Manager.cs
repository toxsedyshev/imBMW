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
        static ISerialPort iBus;

        public static void Init(ISerialPort port)
        {
            messageWriteQueue = new QueueThreadWorker(SendMessage);
            //messageReadQueue = new QueueThreadWorker(ProcessMessage);

            iBus = port;
            iBus.DataReceived += new SerialDataReceivedEventHandler(iBus_DataReceived);
        }

        #region Message reading and processing

        //static QueueThreadWorker messageReadQueue;

        static byte[] messageBuffer = new byte[Message.PacketLengthMax];
        static int messageBufferLength = 0;
        static object bufferSync = new object();

        static void iBus_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            ISerialPort port = (ISerialPort)sender;
            if (port.AvailableBytes == 0)
            {
                Logger.Warning("Available bytes lost! " + port.ToString());
                return;
            }
            lock (bufferSync)
            {
                byte[] data = port.ReadAvailable();
                #if DEBUG
                //Logger.Info(data.ToHex(' '), "<!");
                #endif
                if (messageBufferLength + data.Length > messageBuffer.Length)
                {
                    Logger.Info("Buffer overflow. Extending it. " + port.ToString());
                    byte[] newBuffer = new byte[messageBuffer.Length * 2];
                    Array.Copy(messageBuffer, newBuffer, messageBufferLength);
                    messageBuffer = newBuffer;
                }
                if (data.Length == 1)
                {
                    messageBuffer[messageBufferLength++] = data[0];
                }
                else
                {
                    Array.Copy(data, 0, messageBuffer, messageBufferLength, data.Length);
                    messageBufferLength += data.Length;
                }
                while (messageBufferLength >= Message.PacketLengthMin)
                {
                    Message m = Message.TryCreate(messageBuffer, messageBufferLength);
                    if (m == null)
                    {
                        if (!Message.CanStartWith(messageBuffer, messageBufferLength))
                        {
                            Logger.Warning("Buffer skip: non-iBus data detected: " + messageBuffer[0].ToHex());
                            SkipBuffer(1);
                            continue;
                        }
                        return;
                    }
                    ProcessMessage(m);
                    //#if DEBUG
                    //m.PerformanceInfo.TimeEnqueued = DateTime.Now;
                    //#endif
                    //messageReadQueue.Enqueue(m);
                    SkipBuffer(m.PacketLength);
                }
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

            #if DEBUG
            m.PerformanceInfo.TimeStartedProcessing = DateTime.Now;
            #endif

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

            #if DEBUG
            m.PerformanceInfo.TimeEndedProcessing = DateTime.Now;
            #endif

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

        static void SendMessage(object o)
        {
            Message m = (Message)o;

            #if DEBUG
            m.PerformanceInfo.TimeStartedProcessing = DateTime.Now;
            #endif

            MessageEventArgs args = null;
            var e = BeforeMessageSent;
            if (e != null)
            {
                args = new MessageEventArgs(m);
                e(args);
                if (args.Cancel)
                {
                    return;
                }
            }

            iBus.Write(m.Packet);

            #if DEBUG
            m.PerformanceInfo.TimeEndedProcessing = DateTime.Now;
            #endif

            e = AfterMessageSent;
            if (e != null)
            {
                if (args == null)
                {
                    args = new MessageEventArgs(m);
                }
                e(args);
            }

            Thread.Sleep(iBus.AfterWriteDelay); // Don't flood iBus
        }

        public static void EnqueueMessage(Message m)
        {
            if (iBus is SerialPortHub)
            {
                SendMessage(m);
                return;
            }
            #if DEBUG
            m.PerformanceInfo.TimeEnqueued = DateTime.Now;
            #endif
            messageWriteQueue.Enqueue(m);
        }

        public static void EnqueueMessage(params Message[] messages)
        {
            if (iBus is SerialPortHub)
            {
                foreach (Message m in messages)
                {
                    SendMessage(m);
                }
                return;
            }
            #if DEBUG
            var now = DateTime.Now;
            foreach (Message m in messages)
            {
                m.PerformanceInfo.TimeEnqueued = now;
            }
            #endif
            messageWriteQueue.EnqueueArray(messages);
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
