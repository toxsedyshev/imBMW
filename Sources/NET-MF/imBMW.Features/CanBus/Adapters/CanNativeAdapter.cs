using System;
using Microsoft.SPOT;
using GHI.IO;

namespace imBMW.Features.CanBus.Adapters
{
    public class CanNativeAdapter : CanAdapter
    {
        ControllerAreaNetwork can;

        public CanNativeAdapter(CanAdapterSettings settings)
            : base(settings)
        {
            can = new ControllerAreaNetwork(settings.Port, settings.Speed);
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
                throw new CanException("CAN adapter is not allowed to send the message now");
            }
            can.SendMessage(message.NativeMessage);
        }
    }
}
