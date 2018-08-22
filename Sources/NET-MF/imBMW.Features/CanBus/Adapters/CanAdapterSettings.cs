using System;
using Microsoft.SPOT;
using GHI.IO;

namespace imBMW.Features.CanBus.Adapters
{
    public class CanAdapterSettings
    {
        public readonly ControllerAreaNetwork.Speed Speed;

        public MCP2515.enBaudRate MCP2515Speed
        {
            get
            {
                switch (Speed)
                {
                    case ControllerAreaNetwork.Speed.Kbps125:
                        return MCP2515.enBaudRate.CAN_BAUD_125K;
                    case ControllerAreaNetwork.Speed.Kbps250:
                        return MCP2515.enBaudRate.CAN_BAUD_250K;
                    case ControllerAreaNetwork.Speed.Kbps500:
                        return MCP2515.enBaudRate.CAN_BAUD_500K;
                    default:
                        throw new CanException("Not supported CAN speed.");
                }
            }
        }
    }
}
