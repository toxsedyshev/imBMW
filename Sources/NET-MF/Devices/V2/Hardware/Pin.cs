using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHI.Pins;

namespace imBMW.Devices.V2.Hardware
{
    public class Pin
    {
        /// <summary>
        /// A value indicating that no GPIO pin is specified.
        /// </summary>
        public const Cpu.Pin GPIO_NONE = Cpu.Pin.GPIO_NONE;

        /// <summary>
        /// LED1. Near microSD socket on imBMW V2 main board.
        /// </summary>
        public static Cpu.Pin LED = Generic.GetPin('A', 8);

        /// <summary>
        /// Interrupt port for TH3122 SEN/STA (busy) output.
        /// </summary>
        public static Cpu.Pin TH3122SENSTA = Generic.GetPin('C', 2);

        /// <summary>
        /// Digital I/O.
        /// COM2 RX.
        /// </summary>
        public static Cpu.Pin Di0 = Generic.GetPin('A', 3);

        /// <summary>
        /// Digital I/O.
        /// COM2 TX.
        /// </summary>
        public static Cpu.Pin Di1 = Generic.GetPin('A', 2);

        /// <summary>
        /// Digital I/O.
        /// I2C SDA.
        /// </summary>
        public static Cpu.Pin Di2 = Generic.GetPin('B', 7);

        /// <summary>
        /// Digital I/O.
        /// I2C SCL.
        /// </summary>
        public static Cpu.Pin Di3 = Generic.GetPin('B', 6);

        /// <summary>
        /// Digital I/O.
        /// CAN1 RX.
        /// </summary>
        public static Cpu.Pin Di4 = Generic.GetPin('B', 8);

        /// <summary>
        /// Digital I/O.
        /// COM2 CTS.
        /// </summary>
        public static Cpu.Pin Di5 = Generic.GetPin('A', 0);

        /// <summary>
        /// Digital I/O.
        /// COM2 RTS.
        /// </summary>
        public static Cpu.Pin Di6 = Generic.GetPin('A', 1);

        /// <summary>
        /// Digital I/O.
        /// CAN1 TX.
        /// </summary>
        public static Cpu.Pin Di7 = Generic.GetPin('B', 9);

        /// <summary>
        /// Digital I/O.
        /// COM1 TX.
        /// </summary>
        public static Cpu.Pin Di8 = Generic.GetPin('C', 6);

        /// <summary>
        /// Digital I/O.
        /// COM1 RX.
        /// </summary>
        public static Cpu.Pin Di9 = Generic.GetPin('C', 7);

        /// <summary>
        /// Digital I/O.
        /// </summary>
        public static Cpu.Pin Di10 = Generic.GetPin('A', 7);

        /// <summary>
        /// Digital I/O.
        /// SPI1 MOSI.
        /// </summary>
        public static Cpu.Pin Di11 = Generic.GetPin('B', 5);

        /// <summary>
        /// Digital I/O.
        /// SPI1 MISO.
        /// </summary>
        public static Cpu.Pin Di12 = Generic.GetPin('B', 4);

        /// <summary>
        /// Digital I/O.
        /// SPI1 SCK.
        /// </summary>
        public static Cpu.Pin Di13 = Generic.GetPin('B', 3);

        
        /// <summary>
        /// Digital I/O.
        /// Analog in A0.
        /// </summary>
        public static Cpu.Pin Di14 = Generic.GetPin('A', 6);


        /// <summary>
        /// Digital I/O.
        /// Analog in/out A1.
        /// </summary>
        public static Cpu.Pin Di15 = Generic.GetPin('A', 5);

        /// <summary>
        /// Digital I/O.
        /// Analog in A2.
        /// </summary>
        public static Cpu.Pin Di16 = Generic.GetPin('C', 3);

        /// <summary>
        /// Digital I/O.
        /// Analog in/out A3.
        /// </summary>
        public static Cpu.Pin Di17 = Generic.GetPin('A', 4);

        /// <summary>
        /// Digital I/O.
        /// Analog in A4.
        /// </summary>
        public static Cpu.Pin Di18 = Generic.GetPin('C', 1);

        /// <summary>
        /// Digital I/O.
        /// Analog in A5.
        /// </summary>
        public static Cpu.Pin Di19 = Generic.GetPin('C', 0);

        /// <summary>
        /// Digital I/O.
        /// On the V2 main board under Cerb40.
        /// </summary>
        public static Cpu.Pin Di20 = Generic.GetPin('A', 13);

        /// <summary>
        /// Digital I/O.
        /// On the V2 main board near LED1.
        /// </summary>
        public static Cpu.Pin Di21 = Generic.GetPin('A', 14);
    }
}
