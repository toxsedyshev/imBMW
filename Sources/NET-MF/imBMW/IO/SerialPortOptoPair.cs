/*
using System;
using Microsoft.SPOT;
using System.Threading;

namespace System.IO.Ports
{
    public class SerialPortOptoPair : SerialTimerInterruptPort
    {
        public SerialPortOptoPair(String port) :
            base(new SerialPortConfiguration(port, BaudRate.Baudrate9600, Parity.Even, 8, StopBits.One), 50, 0, 1, Timeout.Infinite)
        {
            AfterWriteDelay = 40;
            IsWriteEchoed = true;
        }
    }
}
*/