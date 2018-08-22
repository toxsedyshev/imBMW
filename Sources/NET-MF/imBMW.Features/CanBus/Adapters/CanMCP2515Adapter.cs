using System;
using Microsoft.SPOT;
using System.Threading;
using System.Collections;

namespace imBMW.Features.CanBus.Adapters
{
    public class CanMCP2515Adapter : CanAdapter
    {
        MCP2515 can;
        Thread receiveThread;
        bool isEnabled;

        public CanMCP2515Adapter(CanAdapterSettings settings) : base(settings)
        {
            can = new MCP2515();
            can.InitCAN(settings.MCP2515Speed);
        }

        public override bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                if (!value)
                {
                    throw new CanException("Can't disable MCP2515 CAN adapter.");
                }
                can.SetCANNormalMode();
                isEnabled = true;
                receiveThread = new Thread(Worker);
                receiveThread.Start();
            }
        }

        private void Worker()
        {
            MCP2515.CANMSG message;
            while (IsEnabled)
            {
                if (can.Receive(out message, 20))
                {
                    OnMessageReceived(new CanMessage(message));
                }
            }
        }

        public override void SendMessage(CanMessage message)
        {
            CheckEnabled();
            can.Transmit(message.MCP2515Message, 10);
        }
    }
}
