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
        public static Cpu.Pin LED = FEZCerb40II.Gpio.PA8;

        /// <summary>
        /// Interrupt port for TH3122 SEN/STA (busy) output.
        /// </summary>
        public static Cpu.Pin TH3122SENSTA = FEZCerb40II.Gpio.PC2;

        /// <summary>
        /// Digital I/O.
        /// COM2 RX.
        /// </summary>
        public static Cpu.Pin Di0 = FEZCerb40II.Gpio.PA3;

        /// <summary>
        /// Digital I/O.
        /// COM2 TX.
        /// </summary>
        public static Cpu.Pin Di1 = FEZCerb40II.Gpio.PA2;

        /// <summary>
        /// Digital I/O.
        /// I2C SDA.
        /// </summary>
        public static Cpu.Pin Di2 = FEZCerb40II.Gpio.PB7;

        /// <summary>
        /// Digital I/O.
        /// I2C SCL.
        /// </summary>
        public static Cpu.Pin Di3 = FEZCerb40II.Gpio.PB6;

        /// <summary>
        /// Digital I/O.
        /// CAN1 RX.
        /// </summary>
        public static Cpu.Pin Di4 = FEZCerb40II.Gpio.PB8;

        /// <summary>
        /// Digital I/O.
        /// COM2 CTS.
        /// </summary>
        public static Cpu.Pin Di5 = FEZCerb40II.Gpio.PA0;

        /// <summary>
        /// Digital I/O.
        /// COM2 RTS.
        /// </summary>
        public static Cpu.Pin Di6 = FEZCerb40II.Gpio.PA1;

        /// <summary>
        /// Digital I/O.
        /// CAN1 TX.
        /// </summary>
        public static Cpu.Pin Di7 = FEZCerb40II.Gpio.PB9;

        /// <summary>
        /// Digital I/O.
        /// COM1 TX.
        /// </summary>
        public static Cpu.Pin Di8 = FEZCerb40II.Gpio.PC6;

        /// <summary>
        /// Digital I/O.
        /// COM1 RX.
        /// </summary>
        public static Cpu.Pin Di9 = FEZCerb40II.Gpio.PC7;

        /// <summary>
        /// Digital I/O.
        /// </summary>
        public static Cpu.Pin Di10 = FEZCerb40II.Gpio.PA7;

        /// <summary>
        /// Digital I/O.
        /// SPI1 MOSI.
        /// </summary>
        public static Cpu.Pin Di11 = FEZCerb40II.Gpio.PB5;

        /// <summary>
        /// Digital I/O.
        /// SPI1 MISO.
        /// </summary>
        public static Cpu.Pin Di12 = FEZCerb40II.Gpio.PB4;

        /// <summary>
        /// Digital I/O.
        /// SPI1 SCK.
        /// </summary>
        public static Cpu.Pin Di13 = FEZCerb40II.Gpio.PB3;


        /// <summary>
        /// Digital I/O.
        /// Analog in A0.
        /// </summary>
        public static Cpu.Pin Di14 = FEZCerb40II.Gpio.PA6;


        /// <summary>
        /// Digital I/O.
        /// Analog in/out A1.
        /// </summary>
        public static Cpu.Pin Di15 = FEZCerb40II.Gpio.PA5;

        /// <summary>
        /// Digital I/O.
        /// Analog in A2.
        /// </summary>
        public static Cpu.Pin Di16 = FEZCerb40II.Gpio.PC3;

        /// <summary>
        /// Digital I/O.
        /// Analog in/out A3.
        /// </summary>
        public static Cpu.Pin Di17 = FEZCerb40II.Gpio.PA4;

        /// <summary>
        /// Digital I/O.
        /// Analog in A4.
        /// </summary>
        public static Cpu.Pin Di18 = FEZCerb40II.Gpio.PC1;

        /// <summary>
        /// Digital I/O.
        /// Analog in A5.
        /// </summary>
        public static Cpu.Pin Di19 = FEZCerb40II.Gpio.PC0;

        /// <summary>
        /// Digital I/O.
        /// On the V2 main board under Cerb40.
        /// </summary>
        public static Cpu.Pin Di20 = FEZCerb40II.Gpio.PA13;

        /// <summary>
        /// Digital I/O.
        /// On the V2 main board near LED1.
        /// </summary>
        public static Cpu.Pin Di21 = FEZCerb40II.Gpio.PA14;

        /// <summary>
        /// SPI1 port.
        /// </summary>
        public static SPI.SPI_module SPI = FEZCerb40II.SpiBus.Spi1;

        /// <summary>
        /// Chip select pin of SPI1.
        /// </summary>
        public static Cpu.Pin SPI_ChipSelect = Di3;

        /// <summary>
        /// CAN BUS port.
        /// </summary>
        public static int CAN = FEZCerb40II.CanBus.Can1;
    }
}
