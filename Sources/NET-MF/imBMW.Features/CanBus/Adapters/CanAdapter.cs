using System;
using Microsoft.SPOT;
using GHI.IO;

namespace imBMW.Features.CanBus.Adapters
{
    public abstract class CanAdapter
    {
        public static CanAdapter Current { get; set; }

        public CanAdapter(CanAdapterSettings settings)
        {
            Settings = settings;
        }

        public delegate void MessageHandler(CanAdapter can, CanMessage message);

        public delegate void ErrorHandler(CanAdapter can, string message);

        public event MessageHandler MessageReceived;

        public event MessageHandler MessageSent;

        public event ErrorHandler Error;

        public abstract bool SendMessage(CanMessage message);

        public CanAdapterSettings Settings { get; protected set; }

        public abstract bool IsEnabled { get; set; }

        protected void OnMessageReceived(CanMessage message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, message);
            }
        }

        protected void OnMessageSent(CanMessage message, bool sent)
        {
            if (sent)
            {
                if (MessageSent != null)
                {
                    MessageSent(this, message);
                }
            }
            else
            {
                OnError("Can't send: " + message);
            }
        }

        protected void OnError(string error)
        {
            if (Error != null)
            {
                Error(this, error);
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
