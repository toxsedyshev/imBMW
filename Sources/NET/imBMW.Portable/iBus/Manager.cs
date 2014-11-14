using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.iBus
{
    public class Manager
    {
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
        public static event MessageEventHandler BeforeMessageEnqueued;

        /// <summary>
        /// Fired on new message enqueued to be sent
        /// </summary>
        public static event MessageEventHandler MessageEnqueued;

        static List<MessageReceiverRegistration> messageReceiverList = new List<MessageReceiverRegistration>();

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

        public static void ProcessMessage(Message m)
        {
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
                    receiver.Process(m);
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

        public static void EnqueueMessage(params Message[] message)
        {
            var e = MessageEnqueued;
            var be = BeforeMessageEnqueued;
            if (e != null)
            {
                foreach (var m in message)
                {
                    var args = new MessageEventArgs(m);

                    if (be != null)
                    {
                        try
                        {
                            be(args);
                            if (args.Cancel)
                            {
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "on before message enqueued " + m.ToPrettyString());
                        }
                    }

                    try
                    {
                        e(args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "on message enqueued " + m.ToPrettyString());
                    }
                }
            }
        }
    }
}
