using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : SerialInterruptPort
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="busy"></param>
        /// <param name="fixParity">Set true if the data is corrupted because of parity bit issue in Cerberus software.</param>
        public SerialPortTH3122(String port, Cpu.Pin busy, bool fixParity = false) :
            base(new SerialPortConfiguration(port, BaudRate.Baudrate9600, Parity.Even, 8 + (fixParity ? 1 : 0), StopBits.One), busy, 0, imBMW.iBus.Message.PacketLengthMax, 50)
        {
            AfterWriteDelay = 20;
        }
    }
}
