using System;
using Microsoft.SPOT;
using GHI.IO;

namespace imBMW.Features.CanBus.Adapters
{
    public class CanNativeAdapter : CanAdapter
    {
        ControllerAreaNetwork can;

        public CanNativeAdapter(ControllerAreaNetwork.Channel canPort, CanAdapterSettings.CanSpeed speed)
            : this(new CanNativeAdapterSettings(canPort, speed))
        { }

        public CanNativeAdapter(ControllerAreaNetwork.Channel canPort, ControllerAreaNetwork.Timings timings)
            : this(new CanNativeAdapterSettings(canPort, 0), timings)
        { }

        protected CanNativeAdapter(CanNativeAdapterSettings settings, ControllerAreaNetwork.Timings timings = null)
            : base(settings)
        {
            switch (settings.Speed)
            {
                case CanAdapterSettings.CanSpeed.Kbps1000:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps1000);
                    break;
                case CanAdapterSettings.CanSpeed.Kbps500:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps500);
                    break;
                case CanAdapterSettings.CanSpeed.Kbps250:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps250);
                    break;
                case CanAdapterSettings.CanSpeed.Kbps125:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps125);
                    break;
                default:
                    can = new ControllerAreaNetwork(settings.CanPort, timings ?? GetTimings(settings));
                    break;
            }
            can.ErrorReceived += Can_ErrorReceived;
            can.MessageAvailable += Can_MessageAvailable;
        }

        public override bool IsEnabled
        {
            get { return can.Enabled; }
            set { can.Enabled = value; }
        }

        private void Can_MessageAvailable(ControllerAreaNetwork sender, ControllerAreaNetwork.MessageAvailableEventArgs e)
        {
            var messages = sender.ReadMessages();
            foreach (var message in messages)
            {
                OnMessageReceived(new CanMessage(message));
            }
        }

        private void Can_ErrorReceived(ControllerAreaNetwork sender, ControllerAreaNetwork.ErrorReceivedEventArgs e)
        {
            OnErrorReceived(e.Error.ToString());
        }

        public override void SendMessage(CanMessage message)
        {
            CheckEnabled();
            if (!can.CanSend)
            {
                throw new CanException("CAN adapter is not allowed to send the message now.");
            }
            can.SendMessage(message.NativeMessage);
        }
        
        private ControllerAreaNetwork.Timings GetTimings(CanAdapterSettings settings)
        {
            switch (ControllerAreaNetwork.SourceClock)
            {
                case 42000000:
                    switch (settings.Speed)
                    {
                        case CanAdapterSettings.CanSpeed.Kbps100:
                            // 21TQ, 75%SP
                            return new ControllerAreaNetwork.Timings(0, 15, 5, 20, 1);
                    }
                    break;
                case 72000000:
                    switch (settings.Speed)
                    {
                        case CanAdapterSettings.CanSpeed.Kbps100:
                            // 24TQ, 65%SP
                            return new ControllerAreaNetwork.Timings(0, 15, 8, 30, 1);
                    }
                    break;
            }
            throw new CanException("Specified baudrate isn't supported for current CAN controller frequency.");
        }
    }
}
