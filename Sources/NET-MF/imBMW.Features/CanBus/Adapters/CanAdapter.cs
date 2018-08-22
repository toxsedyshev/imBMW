using System;
using Microsoft.SPOT;
using GHI.IO;

namespace imBMW.Features.CanBus.Adapters
{
    public abstract class CanAdapter
    {
        public CanAdapter(CanAdapterSettings settings)
        {
            Settings = settings;
        }

        public delegate void MessageReceivedHandler(CanMessage message);

        public delegate void ErrorReceivedHandler(string message);

        public event MessageReceivedHandler MessageReceived;

        public event ErrorReceivedHandler ErrorReceived;

        public abstract void SendMessage(CanMessage message);

        public CanAdapterSettings Settings { get; protected set; }

        public abstract bool IsEnabled { get; set; }

        protected void OnMessageReceived(CanMessage message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(message);
            }
        }

        protected void OnErrorReceived(string error)
        {
            if (ErrorReceived != null)
            {
                ErrorReceived(error);
            }
        }

        internal void CheckEnabled()
        {
            if (!IsEnabled)
            {
                throw new CanException("CAN adapter is not enabled.");
            }
        }
    }
}
