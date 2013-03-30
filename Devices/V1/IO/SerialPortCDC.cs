using System;
using Microsoft.SPOT;
using GHIElectronics.NETMF.USBClient;
using System.Threading;

namespace System.IO.Ports
{
    public class SerialPortCDC : SerialPortBase, ISerialPort
    {
        USBC_CDC port;

        public SerialPortCDC(USBC_CDC port, int writeBufferSize, int readBufferSize)
            : base(writeBufferSize, readBufferSize)
        {
            // TODO Check somewhere USBClientController.GetState() == USBClientController.State.Running

            this.port = port;

            ReadTimeout = 33;
            WriteTimeout = 0;
        }

        public SerialPortCDC(USBC_CDC port) : this(port, 0, 1) { }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            return port.Write(data, offset, length);
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            return port.Read(data, offset, length);
        }

        public override int ReadTimeout
        {
            get
            {
                return base.ReadTimeout;
            }
            set
            {
                port.ReadTimeout = value;
                base.ReadTimeout = value;
            }
        }

        protected override bool CanWrite { get { return true; } }

        public override void Flush() { }
    }
}
