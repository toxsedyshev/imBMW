using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using FEZPin = GHI.Hardware.FEZCerb.Pin;

namespace imBMW.Devices.V2.Hardware
{
    public class Pin
    {
        // Summary:
        //     A value indicating that no GPIO pin is specified.
        public const Cpu.Pin GPIO_NONE = FEZPin.GPIO_NONE;
        //
        // Summary:
        //     Digital I/O.
        //     COM2 RX.
        public const Cpu.Pin Di0 = FEZPin.PA3;
        //
        // Summary:
        //     Digital I/O.
        //     COM2 TX.
        public const Cpu.Pin Di1 = FEZPin.PA2;
        //
        // Summary:
        //     Digital I/O.
        //     I2C SDA.
        public const Cpu.Pin Di2 = FEZPin.PB7;
        //
        // Summary:
        //     Digital I/O.
        //     I2C SCL.
        public const Cpu.Pin Di3 = FEZPin.PB6;
        //
        // Summary:
        //     Digital I/O.
        //     CAN1 RX.
        public const Cpu.Pin Di4 = FEZPin.PB8;
        //
        // Summary:
        //     Digital I/O.
        //     COM2 CTS.
        public const Cpu.Pin Di5 = FEZPin.PA0;
        //
        // Summary:
        //     Digital I/O.
        //     COM2 RTS.
        public const Cpu.Pin Di6 = FEZPin.PA1;
        //
        // Summary:
        //     Digital I/O.
        //     CAN1 TX.
        public const Cpu.Pin Di7 = FEZPin.PB9;
        //
        // Summary:
        //     Digital I/O.
        //     COM1 TX.
        public const Cpu.Pin Di8 = FEZPin.PC6;
        //
        // Summary:
        //     Digital I/O.
        //     COM1 RX.
        public const Cpu.Pin DI9 = FEZPin.PC7;
        //
        // Summary:
        //     Digital I/O.
        public const Cpu.Pin Di10 = FEZPin.PA7;
        //
        // Summary:
        //     Digital I/O.
        //     SPI1 MOSI.
        public const Cpu.Pin Di11 = FEZPin.PB5;
        //
        // Summary:
        //     Digital I/O.
        //     SPI1 MISO.
        public const Cpu.Pin Di12 = FEZPin.PB4;
        //
        // Summary:
        //     Digital I/O.
        //     SPI1 SCK.
        public const Cpu.Pin Di13 = FEZPin.PB3;
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A0.
        public const Cpu.Pin Di14 = FEZPin.PA6;
        //
        // Summary:
        //     Digital I/O.
        //     Analog in/out A1.
        public const Cpu.Pin Di15 = FEZPin.PA5;
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A2.
        public const Cpu.Pin Di16 = FEZPin.PC3;
        //
        // Summary:
        //     Digital I/O.
        //     Analog in/out A3.
        public const Cpu.Pin Di17 = FEZPin.PA4;
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A4.
        public const Cpu.Pin Di18 = FEZPin.PC1;
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A5.
        public const Cpu.Pin Di19 = FEZPin.PC0;
        //
        // Summary:
        //     Digital I/O.
        //     On the V2 main board under Cerb40.
        public const Cpu.Pin Di20 = FEZPin.PA13;
        //
        // Summary:
        //     Digital I/O.
        //     On the V2 main board near LED1.
        public const Cpu.Pin Di21 = FEZPin.PA14;
    }
}
