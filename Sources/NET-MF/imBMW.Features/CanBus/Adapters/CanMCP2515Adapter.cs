using System;
using Microsoft.SPOT;
using System.Threading;
using System.Collections;
using Microsoft.SPOT.Hardware;

namespace imBMW.Features.CanBus.Adapters
{
    public class CanMCP2515Adapter : CanAdapter
    {
        MCP2515 can;
        Thread receiveThread;
        bool isEnabled;

        public CanMCP2515Adapter(SPI.SPI_module spi, Cpu.Pin chipSelect, CanAdapterSettings.CanSpeed speed, CanMCP2515AdapterSettings.AdapterFrequency frequency)
            : this(new CanMCP2515AdapterSettings(spi, chipSelect, speed, frequency))
        { }

        public CanMCP2515Adapter(CanMCP2515AdapterSettings settings) : base(settings)
        {
            can = new MCP2515();
            can.InitCAN(settings.SPI, settings.ChipSelect, GetTimings(settings));
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
        
        private byte[] GetTimings(CanMCP2515AdapterSettings settings)
        {
            switch (settings.Speed)
            {
                case CanAdapterSettings.CanSpeed.Kbps1000:
                    if (settings.Frequency == CanMCP2515AdapterSettings.AdapterFrequency.Mhz16)
                    {
                        return new byte[] { 0x00, 0x91, 0x01 };
                    }
                    else
                    {
                        return new byte[] { 0x00, 0x80, 0x00 };
                    }
                case CanAdapterSettings.CanSpeed.Kbps500:
                    if (settings.Frequency == CanMCP2515AdapterSettings.AdapterFrequency.Mhz16)
                    {
                        return new byte[] { 0x00, 0xAC, 0x03 };
                    }
                    else
                    {
                        return new byte[] { 0x00, 0x91, 0x01 };
                    }
                case CanAdapterSettings.CanSpeed.Kbps250:
                    if (settings.Frequency == CanMCP2515AdapterSettings.AdapterFrequency.Mhz16)
                    {
                        return new byte[] { 0x01, 0xAC, 0x03 };
                    }
                    else
                    {
                        return new byte[] { 0x00, 0xAC, 0x03 };
                    }
                case CanAdapterSettings.CanSpeed.Kbps125:
                    if (settings.Frequency == CanMCP2515AdapterSettings.AdapterFrequency.Mhz16)
                    {
                        return new byte[] { 0x03, 0xAC, 0x03 };
                    }
                    else
                    {
                        return new byte[] { 0x01, 0xAC, 0x03 };
                    }
                case CanAdapterSettings.CanSpeed.Kbps100:
                    if (settings.Frequency == CanMCP2515AdapterSettings.AdapterFrequency.Mhz16)
                    {
                        return new byte[] { 0x03, 0xB6, 0x04 };
                    }
                    else
                    {
                        return new byte[] { 0x01, 0xB6, 0x04 };
                    }
                default:
                    throw new CanException("Specified baudrate isn't supported.");
            }
        }
    }
}
