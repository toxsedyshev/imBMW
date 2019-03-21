using System;
using Microsoft.SPOT.Hardware;
using GHI.IO;
using System.IO.Ports;
using GHI.IO.Storage;

namespace imBMW.Devices.V2.Hardware
{
    public class Pin
    {
        #region Constants

        /// <summary>
        /// The analog input precision supported by the board.
        /// </summary>
        public const int SupportedAnalogInputPrecision = 12;

        /// <summary>
        /// The analog output precision supported by the board.
        /// </summary>
        public const int SupportedAnalogOutputPrecision = 12;

        #endregion

        #region Interfaces

        /// <summary>
        /// A value indicating that no GPIO pin is specified.
        /// </summary>
        public const Cpu.Pin GPIO_NONE = Cpu.Pin.GPIO_NONE;

        /// <summary>
        /// LED1. Near microSD socket on imBMW V2 main board.
        /// PA8 = PWM.
        /// </summary>
        public const Cpu.Pin LED = Cpu.Pin.GPIO_Pin8;

        /// <summary>
        /// TH3122 iBus port.
        /// </summary>
        public const string TH3122Port = Serial.COM3;

        /// <summary>
        /// Interrupt port for TH3122 SEN/STA (busy) output.
        /// PC2.
        /// </summary>
        public const Cpu.Pin TH3122SENSTA = (Cpu.Pin)34;

        /// <summary>
        /// SPI1 port.
        /// </summary>
        public const SPI.SPI_module SPI1 = SPI.SPI_module.SPI1;

        /// <summary>
        /// Chip select pin of SPI1.
        /// </summary>
        public const Cpu.Pin SPI1_ChipSelect = Di3;

        /// <summary>
        /// CAN BUS port.
        /// </summary>
        public const ControllerAreaNetwork.Channel CAN1 = (ControllerAreaNetwork.Channel)1;

        /// <summary>
        /// SD card interface.
        /// </summary>
        public const SDCard.SDInterface SDInterface = SDCard.SDInterface.MCI;

        /// <summary>
        /// Serial port on PC6 (TX) and PC7 (RX).
        /// </summary>
        public const string Com1 = "COM1";

        /// <summary>
        /// Serial port on PA2 (TX), PA3 (RX), PA0 (CTS), and PA1 (RTS)
        /// </summary>
        public const string Com2 = "COM2";

        #endregion

        #region Digital

        /// <summary>
        /// Digital I/O.
        /// PA3 = COM2 RX.
        /// </summary>
        public const Cpu.Pin Di0 = Cpu.Pin.GPIO_Pin3;

        /// <summary>
        /// Digital I/O.
        /// PA2 = COM2 TX.
        /// </summary>
        public const Cpu.Pin Di1 = Cpu.Pin.GPIO_Pin2;

        /// <summary>
        /// Digital I/O.
        /// PB7 = I2C SDA.
        /// </summary>
        public const Cpu.Pin Di2 = (Cpu.Pin)23;

        /// <summary>
        /// Digital I/O.
        /// PB6 = I2C SCL.
        /// </summary>
        public const Cpu.Pin Di3 = (Cpu.Pin)22;

        /// <summary>
        /// Digital I/O.
        /// PB8 = CAN1 RX.
        /// </summary>
        public const Cpu.Pin Di4 = (Cpu.Pin)24;

        /// <summary>
        /// Digital I/O.
        /// PA0 = COM2 CTS.
        /// </summary>
        public const Cpu.Pin Di5 = Cpu.Pin.GPIO_Pin0;

        /// <summary>
        /// Digital I/O.
        /// PA1 = COM2 RTS.
        /// </summary>
        public const Cpu.Pin Di6 = Cpu.Pin.GPIO_Pin1;

        /// <summary>
        /// Digital I/O.
        /// PB9 = CAN1 TX.
        /// </summary>
        public const Cpu.Pin Di7 = (Cpu.Pin)25;

        /// <summary>
        /// Digital I/O.
        /// PC6 = COM1 TX / PWM.
        /// </summary>
        public const Cpu.Pin Di8 = (Cpu.Pin)38;

        /// <summary>
        /// Digital I/O.
        /// PC7 = COM1 RX / PWM.
        /// </summary>
        public const Cpu.Pin Di9 = (Cpu.Pin)39;

        /// <summary>
        /// Digital I/O.
        /// PA7 = PWM.
        /// </summary>
        public const Cpu.Pin Di10 = Cpu.Pin.GPIO_Pin7;

        /// <summary>
        /// Digital I/O.
        /// PB5 = SPI1 MOSI / PWM.
        /// </summary>
        public const Cpu.Pin Di11 = (Cpu.Pin)21;

        /// <summary>
        /// Digital I/O.
        /// PB4 = SPI1 MISO / PWM.
        /// </summary>
        public const Cpu.Pin Di12 = (Cpu.Pin)20;

        /// <summary>
        /// Digital I/O.
        /// PB3 = SPI1 SCK / PWM.
        /// </summary>
        public const Cpu.Pin Di13 = (Cpu.Pin)19;


        /// <summary>
        /// Digital I/O.
        /// PA6 = Analog in An0.
        /// </summary>
        public const Cpu.Pin Di14 = Cpu.Pin.GPIO_Pin6;


        /// <summary>
        /// Digital I/O.
        /// PA5 = Analog in/out An1/AnOut1.
        /// </summary>
        public const Cpu.Pin Di15 = Cpu.Pin.GPIO_Pin5;

        /// <summary>
        /// Digital I/O.
        /// PC3 = Analog in An2.
        /// </summary>
        public const Cpu.Pin Di16 = (Cpu.Pin)35;

        /// <summary>
        /// Digital I/O.
        /// PA4 = Analog in An3 / out AnOut0.
        /// </summary>
        public const Cpu.Pin Di17 = Cpu.Pin.GPIO_Pin4;

        /// <summary>
        /// Digital I/O.
        /// PC1 = Analog in An4.
        /// </summary>
        public const Cpu.Pin Di18 = (Cpu.Pin)33;

        /// <summary>
        /// Digital I/O.
        /// PC0 = Analog in An5.
        /// </summary>
        public const Cpu.Pin Di19 = (Cpu.Pin)32;

        /// <summary>
        /// Digital I/O.
        /// On the V2 main board under Cerb40.
        /// PA13 = JTAG SWD SWDIO.
        /// </summary>
        public const Cpu.Pin Di20 = Cpu.Pin.GPIO_Pin13;

        /// <summary>
        /// Digital I/O.
        /// On the V2 main board near LED1.
        /// PA14 = JTAG SWD CLK.
        /// </summary>
        public const Cpu.Pin Di21 = Cpu.Pin.GPIO_Pin14;

        /// <summary>
        /// PWM channel.
        /// PA7.
        /// </summary>
        public const Cpu.PWMChannel Di10PWM = Cpu.PWMChannel.PWM_1;

        /// <summary>
        /// PWM channel.
        /// PA8.
        /// </summary>
        public const Cpu.PWMChannel LED1PWM = Cpu.PWMChannel.PWM_3;

        /// <summary>
        /// PWM channel.
        /// PB3.
        /// </summary>
        public const Cpu.PWMChannel Di13PWM = (Cpu.PWMChannel)8;

        /// <summary>
        /// PWM channel.
        /// PB4.
        /// </summary>
        public const Cpu.PWMChannel Di12PWM = Cpu.PWMChannel.PWM_7;

        /// <summary>
        /// PWM channel.
        /// PB5.
        /// </summary>
        public const Cpu.PWMChannel Di11PWM = Cpu.PWMChannel.PWM_6;

        /// <summary>
        /// PWM channel.
        /// PC6.
        /// </summary>
        public const Cpu.PWMChannel Di8PWM = Cpu.PWMChannel.PWM_0;

        /// <summary>
        /// PWM channel.
        /// PC7.
        /// </summary>
        public const Cpu.PWMChannel Di9PWM = Cpu.PWMChannel.PWM_2;

        #endregion

        #region Analog

        /// <summary>
        /// Analog input.
        /// Di14 = PA6.
        /// </summary>
        public const Cpu.AnalogChannel An0 = Cpu.AnalogChannel.ANALOG_0;

        /// <summary>
        /// Analog input.
        /// Di15 = PA5 = analog output.
        /// </summary>
        public const Cpu.AnalogChannel An1 = (Cpu.AnalogChannel)8;

        /// <summary>
        /// Analog input.
        /// Di16 = PC3.
        /// </summary>
        public const Cpu.AnalogChannel An2 = Cpu.AnalogChannel.ANALOG_7;

        /// <summary>
        /// Analog input.
        /// Di17 = PA4 = analog output.
        /// </summary>
        public const Cpu.AnalogChannel An3 = Cpu.AnalogChannel.ANALOG_5;

        /// <summary>
        /// Analog input.
        /// Di18 = PC1.
        /// </summary>
        public const Cpu.AnalogChannel An4 = Cpu.AnalogChannel.ANALOG_4;

        /// <summary>
        /// Analog input.
        /// Di19 = PC0.
        /// </summary>
        public const Cpu.AnalogChannel An5 = Cpu.AnalogChannel.ANALOG_3;

        /// <summary>
        /// Analog input.
        /// Di1 = PA2 = COM2 TX.
        /// </summary>
        public const Cpu.AnalogChannel An6 = Cpu.AnalogChannel.ANALOG_1;

        /// <summary>
        /// Analog input.
        /// Di0 = PA3 = COM2 RX.
        /// </summary>
        public const Cpu.AnalogChannel An7 = Cpu.AnalogChannel.ANALOG_2;

        /// <summary>
        /// Analog input.
        /// Already used: PC2 = TH3122SENSTA.
        /// </summary>
        [Obsolete("Already used by TH3122SENSTA.")]
        public const Cpu.AnalogChannel An8 = Cpu.AnalogChannel.ANALOG_6;

        /// <summary>
        /// Analog output.
        /// Di17 = PA4 = analog input.
        /// </summary>
        public const Cpu.AnalogOutputChannel AnOut0 = Cpu.AnalogOutputChannel.ANALOG_OUTPUT_0;

        /// <summary>
        /// Analog output.
        /// Di15 = PA5 = analog input.
        /// </summary>
        public const Cpu.AnalogOutputChannel AnOut1 = Cpu.AnalogOutputChannel.ANALOG_OUTPUT_1;

        #endregion
    }
}
