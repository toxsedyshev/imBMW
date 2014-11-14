using System;

namespace imBMW.iBus
{

    public delegate void MessageEventHandler(MessageEventArgs e);

    public delegate void DeviceFindHandler(DeviceAddress d, bool found);

    public delegate void MessageReceiver(Message message);

    public class MessageEventArgs
    {
        public Message Message { get; private set; }

        public bool Cancel { get; set; }

        public MessageEventArgs(Message message)
        {
            Message = message;
        }
    }

    public class MessageReceiverRegistration
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

        public void Process(Message m)
        {
            if (Matches(m))
            {
                Callback(m);
            }
        }

        public bool Matches(Message m)
        {
            switch (Match)
            {
                case MessageReceiverRegistration.MatchType.Source:
                    if (Source == m.SourceDevice)
                    {
                        return true;
                    }
                    break;
                case MessageReceiverRegistration.MatchType.Destination:
                    if (Destination == m.DestinationDevice
                        || m.DestinationDevice == DeviceAddress.Broadcast
                        || m.DestinationDevice == DeviceAddress.GlobalBroadcastAddress)
                    {
                        return true;
                    }
                    break;
                case MessageReceiverRegistration.MatchType.SourceAndDestination:
                    if (Source == m.SourceDevice
                        && (Destination == m.DestinationDevice
                            || m.DestinationDevice == DeviceAddress.Broadcast
                            || m.DestinationDevice == DeviceAddress.GlobalBroadcastAddress))
                    {
                        return true;
                    }
                    break;
                case MessageReceiverRegistration.MatchType.SourceOrDestination:
                    if (Source == m.SourceDevice
                        || Destination == m.DestinationDevice
                        || m.DestinationDevice == DeviceAddress.Broadcast
                        || m.DestinationDevice == DeviceAddress.GlobalBroadcastAddress)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }
    }
}
