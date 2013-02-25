using System;
using Microsoft.SPOT;
using System.Collections;
using Microsoft.SPOT.Hardware;
using System.IO.Ports;
using System.Threading;
using GHIElectronics.NETMF.FEZ;
using imBMW.Tools;

namespace imBMW.iBus
{
    static class Manager
    {
        static SerialInterruptPort iBus;

        public static void Init(String port, Cpu.Pin busy)
        {
            iBus = new SerialInterruptPort(new SerialPortConfiguration(port, 9600, Parity.Even, 8, StopBits.One), busy, 0, 1);
            iBus.DataReceived += new SerialDataReceivedEventHandler(iBus_DataReceived);

            messageQueue = new QueueThreadWorker(SendMessage);
        }

        #region Message reading and processing

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
                        Logger.Warning("Buffer overflow. Non-iBus data detected.");
                        SkipBuffer(1);
                        continue;
                    }
                    return;
                }
                ProcessMessage(m);
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

        public static void ProcessMessage(Message m)
        {
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
        }

        #endregion

        #region Message writing and queue

        static QueueThreadWorker messageQueue;

        static void SendMessage(object m)
        {
            iBus.Write((byte[])m);
            Thread.Sleep(50); // Don't flood iBus
        }

        public static void EnqueueMessage(Message m)
        {
            EnqueueMessage(m.Packet);
        }

        public static void EnqueueMessage(byte[] m)
        {
            messageQueue.Enqueue(m);
        }

        #endregion

        #region Message receiver registration

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
