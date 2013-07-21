using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : SerialInterruptPort
    {
        public SerialPortTH3122(String port, Cpu.Pin busy) :
            base(new SerialPortConfiguration(port, 9600, Parity.Even, 8, StopBits.One), busy, 0, imBMW.iBus.Message.PacketLengthMax, 50)
        {
            AfterWriteDelay = 20;
        }
    }
}
