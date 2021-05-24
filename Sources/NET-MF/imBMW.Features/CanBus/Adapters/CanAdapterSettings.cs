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

        public bool UseReadQueue { get; protected set; }

        public CanAdapterSettings(CanSpeed speed, bool useReadQueue)
        {
            Speed = speed;
            UseReadQueue = useReadQueue;
        }
    }

    public class CanNativeAdapterSettings : CanAdapterSettings
    {
        public ControllerAreaNetwork.Channel CanPort { get; private set; }

        public CanNativeAdapterSettings(ControllerAreaNetwork.Channel canPort, CanSpeed speed, bool useReadQueue = false)
            : base (speed, useReadQueue)
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

        public Cpu.Pin Interrupt { get; private set; }

        public CanMCP2515AdapterSettings(SPI.SPI_module spi, Cpu.Pin chipSelect, Cpu.Pin interrupt, CanSpeed speed, AdapterFrequency frequency, bool useReadQueue = false)
            : base(speed, useReadQueue)
        {
            SPI = spi;
            ChipSelect = chipSelect;
            Interrupt = interrupt;
            Frequency = frequency;
        }
    }
}
