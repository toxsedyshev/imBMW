namespace System.IO.Ports
{
    public class SerialPortConfiguration
    {
        public SerialPortConfiguration(string portName, BaudRate baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
        }

        public SerialPortConfiguration(string portName, BaudRate baudRate, Parity parity, int dataBits) : this(portName, baudRate, parity, dataBits, StopBits.One) { }

        public SerialPortConfiguration(string portName, BaudRate baudRate, Parity parity) : this(portName, baudRate, parity, 8) { }

        public SerialPortConfiguration(string portName, BaudRate baudRate) : this(portName, baudRate, Parity.None) { }

        public StopBits StopBits { get; set; }

        public int DataBits { get; set; }

        public Parity Parity { get; set; }

        public BaudRate BaudRate { get; set; }

        public string PortName { get; set; }
    }
}
