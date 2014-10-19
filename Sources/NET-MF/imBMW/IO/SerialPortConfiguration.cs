using System;
using Microsoft.SPOT;

namespace System.IO.Ports
{
    public class SerialPortConfiguration
    {
        string portName;
        BaudRate baudRate;
        Parity parity;
        int dataBits;
        StopBits stopBits;

        public SerialPortConfiguration(string portName, BaudRate baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.portName = portName;
            this.baudRate = baudRate;
            this.parity = parity;
            this.dataBits = dataBits;
            this.stopBits = stopBits;
        }

        public SerialPortConfiguration(string portName, BaudRate baudRate, Parity parity, int dataBits) : this(portName, baudRate, parity, dataBits, StopBits.One) { }

        public SerialPortConfiguration(string portName, BaudRate baudRate, Parity parity) : this(portName, baudRate, parity, 8) { }

        public SerialPortConfiguration(string portName, BaudRate baudRate) : this(portName, baudRate, Parity.None) { }

        public StopBits StopBits
        {
            get { return stopBits; }
            set { stopBits = value; }
        }

        public int DataBits
        {
            get { return dataBits; }
            set { dataBits = value; }
        }

        public Parity Parity
        {
            get { return parity; }
            set { parity = value; }
        }

        public BaudRate BaudRate
        {
            get { return baudRate; }
            set { baudRate = value; }
        }

        public string PortName
        {
            get { return portName; }
            set { portName = value; }
        }
    }
}
