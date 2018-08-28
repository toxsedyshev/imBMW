using System;
using Microsoft.SPOT;
using GHI.IO;
using Microsoft.SPOT.Hardware;

namespace imBMW.Features.CanBus.Adapters
{
    public class CanAdapterSettings
    {
        public enum CanSpeed
        {
            Kbps1000 = 1,
            Kbps500 = 2,
            Kbps250 = 3,
            Kbps125 = 4,
            Kbps100 = 5
        }

        public CanSpeed Speed { get; protected set; }

        public CanAdapterSettings(CanSpeed speed)
        {
            Speed = speed;
        }
    }

    public class CanNativeAdapterSettings : CanAdapterSettings
    {
        public ControllerAreaNetwork.Channel CanPort { get; private set; }

        public CanNativeAdapterSettings(ControllerAreaNetwork.Channel canPort, CanSpeed speed)
            : base (speed)
        {
            CanPort = canPort;
        }
    }

    public class CanMCP2515AdapterSettings : CanAdapterSettings
    {
        public enum AdapterFrequency
        {
            Mhz16,
            Mhz8
        }

        public AdapterFrequency Frequency { get; private set; }

        public SPI.SPI_module SPI { get; private set; }

        public Cpu.Pin ChipSelect { get; private set; }

        public CanMCP2515AdapterSettings(SPI.SPI_module spi, Cpu.Pin chipSelect, CanSpeed speed, AdapterFrequency frequency)
            : base(speed)
        {
            SPI = spi;
            ChipSelect = chipSelect;
            Frequency = frequency;
        }
    }
}
