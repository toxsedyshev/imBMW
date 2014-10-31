using System;
using Microsoft.SPOT;

namespace System.IO.Ports
{
    public class SerialPortConfiguration
    {
        public SerialPortConfiguration(string portName, BaudRate baudRate, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One, bool hardwareFlowControl = false)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
            HardwareFlowControl = hardwareFlowControl;
        }

        public StopBits StopBits { get; protected set; }

        public int DataBits { get; protected set; }

        public Parity Parity { get; protected set; }

        public BaudRate BaudRate { get; protected set; }

        public string PortName { get; protected set; }

        public bool HardwareFlowControl { get; protected set; }
    }
}
