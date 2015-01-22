using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHI.Pins;

namespace imBMW.Devices.V2.Hardware
{
    public class Pin
    {
        // Summary:
        //     A value indicating that no GPIO pin is specified.
        public const Cpu.Pin GPIO_NONE = Cpu.Pin.GPIO_NONE;
        //
        // Summary:
        //     Digital I/O.
        //     COM2 RX.
        public static Cpu.Pin Di0 = Generic.GetPin('A', 3);
        //
        // Summary:
        //     Digital I/O.
        //     COM2 TX.
        public static Cpu.Pin Di1 = Generic.GetPin('A', 2);
        //
        // Summary:
        //     Digital I/O.
        //     I2C SDA.
        public static Cpu.Pin Di2 = Generic.GetPin('B', 7);
        //
        // Summary:
        //     Digital I/O.
        //     I2C SCL.
        public static Cpu.Pin Di3 = Generic.GetPin('B', 6);
        //
        // Summary:
        //     Digital I/O.
        //     CAN1 RX.
        public static Cpu.Pin Di4 = Generic.GetPin('B', 8);
        //
        // Summary:
        //     Digital I/O.
        //     COM2 CTS.
        public static Cpu.Pin Di5 = Generic.GetPin('A', 0);
        //
        // Summary:
        //     Digital I/O.
        //     COM2 RTS.
        public static Cpu.Pin Di6 = Generic.GetPin('A', 1);
        //
        // Summary:
        //     Digital I/O.
        //     CAN1 TX.
        public static Cpu.Pin Di7 = Generic.GetPin('B', 9);
        //
        // Summary:
        //     Digital I/O.
        //     COM1 TX.
        public static Cpu.Pin Di8 = Generic.GetPin('C', 6);
        //
        // Summary:
        //     Digital I/O.
        //     COM1 RX.
        public static Cpu.Pin Di9 = Generic.GetPin('C', 7);
        //
        // Summary:
        //     Digital I/O.
        public static Cpu.Pin Di10 = Generic.GetPin('A', 7);
        //
        // Summary:
        //     Digital I/O.
        //     SPI1 MOSI.
        public static Cpu.Pin Di11 = Generic.GetPin('B', 5);
        //
        // Summary:
        //     Digital I/O.
        //     SPI1 MISO.
        public static Cpu.Pin Di12 = Generic.GetPin('B', 4);
        //
        // Summary:
        //     Digital I/O.
        //     SPI1 SCK.
        public static Cpu.Pin Di13 = Generic.GetPin('B', 3);
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A0.
        public static Cpu.Pin Di14 = Generic.GetPin('A', 6);
        //
        // Summary:
        //     Digital I/O.
        //     Analog in/out A1.
        public static Cpu.Pin Di15 = Generic.GetPin('A', 5);
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A2.
        public static Cpu.Pin Di16 = Generic.GetPin('C', 3);
        //
        // Summary:
        //     Digital I/O.
        //     Analog in/out A3.
        public static Cpu.Pin Di17 = Generic.GetPin('A', 4);
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A4.
        public static Cpu.Pin Di18 = Generic.GetPin('C', 1);
        //
        // Summary:
        //     Digital I/O.
        //     Analog in A5.
        public static Cpu.Pin Di19 = Generic.GetPin('C', 0);
        //
        // Summary:
        //     Digital I/O.
        //     On the V2 main board under Cerb40.
        public static Cpu.Pin Di20 = Generic.GetPin('A', 13);
        //
        // Summary:
        //     Digital I/O.
        //     On the V2 main board near LED1.
        public static Cpu.Pin Di21 = Generic.GetPin('A', 14);
    }
}
