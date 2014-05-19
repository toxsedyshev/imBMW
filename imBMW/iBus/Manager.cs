using System;
using Microsoft.SPOT;
using System.Collections;
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

    public delegate void DeviceFindHandler(DeviceAddress d, bool found);

    #endregion

    public static class Manager
    {
        static ISerialPort _iBus;

        public static void Init(ISerialPort port)
        {
            _messageWriteQueue = new QueueThreadWorker(SendMessage);
            //messageReadQueue = new QueueThreadWorker(ProcessMessage);

            _iBus = port;
            _iBus.DataReceived += iBus_DataReceived;
        }

        #region Message reading and processing

        //static QueueThreadWorker messageReadQueue;

        const int MessageReadTimeout = 16; // 2 * 30 * Message.PacketLengthMax / 8; // tested on 8byte message, got 30ms (real, instead of theoretical 8ms), and made 2x reserve
        static DateTime _lastMessage = DateTime.Now;
        static byte[] _messageBuffer = new byte[Message.PacketLengthMax];
        static int _messageBufferLength;
        static readonly object BufferSync = new object();

        static void iBus_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (ISerialPort)sender;
            if (port.AvailableBytes == 0)
            {
                Logger.Warning("Available bytes lost! " + port);
                return;
            }
            lock (BufferSync)
            {
                byte[] data = port.ReadAvailable();
                #if DEBUG
                //Logger.Info(data.ToHex(' '), "<!");
                #endif
                /*#if DEBUG // TODO remove #if after tests
                if (messageBufferLength > 0)
                {
                    Logger.Warning("WD");
                    var elapsed = (DateTime.Now - lastMessage).GetTotalMilliseconds();
                    if (elapsed > messageReadTimeout)
                    {
                        Logger.Warning("Buffer skip: timeout ("+elapsed+"ms) data: " + messageBuffer.SkipAndTake(0, messageBufferLength).ToHex(" "));
                        messageBufferLength = 0;
                    }
                }
                #endif*/
                if (_messageBufferLength + data.Length > _messageBuffer.Length)
                {
                    Logger.Info("Buffer overflow. Extending it. " + port);
                    var newBuffer = new byte[_messageBuffer.Length * 2];
                    Array.Copy(_messageBuffer, newBuffer, _messageBufferLength);
                    _messageBuffer = newBuffer;
                }
                if (data.Length == 1)
                {
                    _messageBuffer[_messageBufferLength++] = data[0];
                }
                else
                {
                    Array.Copy(data, 0, _messageBuffer, _messageBufferLength, data.Length);
                    _messageBufferLength += data.Length;
                }
                while (_messageBufferLength >= Message.PacketLengthMin)
                {
                    Message m = Message.TryCreate(_messageBuffer, _messageBufferLength);
                    if (m == null)
                    {
                        if (!Message.CanStartWith(_messageBuffer, _messageBufferLength))
                        {
                            Logger.Warning("Buffer skip: non-iBus data detected: " + _messageBuffer[0].ToHex());
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
                _lastMessage = DateTime.Now;
            }
        }

        static void SkipBuffer(byte count)
        {
            _messageBufferLength -= count;
            if (_messageBufferLength > 0)
            {
                Array.Copy(_messageBuffer, count, _messageBuffer, 0, _messageBufferLength);
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

            foreach (MessageReceiverRegistration receiver in MessageReceiverList)
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

        static QueueThreadWorker _messageWriteQueue;

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

            _iBus.Write(m.Packet);

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

            Thread.Sleep(_iBus.AfterWriteDelay); // Don't flood iBus
        }

        public static void EnqueueMessage(Message m)
        {
            if (_iBus is SerialPortHub)
            {
                SendMessage(m);
                return;
            }
            #if DEBUG
            m.PerformanceInfo.TimeEnqueued = DateTime.Now;
            #endif
            _messageWriteQueue.Enqueue(m);
        }

        public static void EnqueueMessage(params Message[] messages)
        {
            if (_iBus is SerialPortHub)
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
            _messageWriteQueue.EnqueueArray(messages);
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

        static readonly ArrayList MessageReceiverList = new ArrayList();

        public static void AddMessageReceiverForSourceDevice(DeviceAddress source, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(source, DeviceAddress.Unset, callback, MessageReceiverRegistration.MatchType.Source));
        }

        public static void AddMessageReceiverForDestinationDevice(DeviceAddress destination, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(DeviceAddress.Unset, destination, callback, MessageReceiverRegistration.MatchType.Destination));
        }

        public static void AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(source, destination, callback, MessageReceiverRegistration.MatchType.SourceAndDestination));
        }

        public static void AddMessageReceiverForSourceOrDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(source, destination, callback, MessageReceiverRegistration.MatchType.SourceOrDestination));
        }

        #endregion

        #region Device searching on iBus

        const int FindDeviceTimeout = 2000;

        static readonly ArrayList FoundDevices = new ArrayList();

        public static bool FindDevice(DeviceAddress device)
        {
            return FindDevice(device, FindDeviceTimeout);
        }

        public static bool FindDevice(DeviceAddress device, int timeout)
        {
            if (FoundDevices.Contains(device))
            {
                return true;
            }
            lock (FoundDevices)
            {
                AfterMessageReceived += SaveFoundDevice;
                // TODO check broadcast poll request              // device
                EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.Broadcast, MessageRegistry.DataPollRequest));
                Thread.Sleep(timeout);
                AfterMessageReceived -= SaveFoundDevice;
                return FoundDevices.Contains(device);
            }
        }

        static void SaveFoundDevice(MessageEventArgs e)
        {
            if (!FoundDevices.Contains(e.Message.SourceDevice))
            {
                FoundDevices.Add(e.Message.SourceDevice);
            }
        }

        #endregion
    }
}
