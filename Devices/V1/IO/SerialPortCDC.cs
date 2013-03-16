using System;
using Microsoft.SPOT;
using GHIElectronics.NETMF.USBClient;
using System.Threading;

namespace System.IO.Ports
{
    public class SerialPortCDC : SerialPortBase, ISerialPort
    {
        /// <summary>
        /// Number of milliseconds to pause the writing thread after sending the write buffer.
        /// </summary>
        public override int WriteTimeout
        {
            get { return port.WriteTimeout; }
            set { port.WriteTimeout = value; }
        }

        /// <summary>
        /// Number of milliseconds to wait for data in read methods. Default is <see cref="Timeout.Infinite"/>. Pass zero to make the read methods return immediately when no data are buffered.
        /// </summary>
        public override int ReadTimeout
        {
            get { return port.ReadTimeout; }
            set
            {
                if (IsReading)
                {
                    return;
                }
                port.ReadTimeout = value;
            }
        }

        USBC_CDC port;

        public SerialPortCDC(USBC_CDC port, int writeBufferSize, int readBufferSize)
        {
            // TODO Check somewhere USBClientController.GetState() == USBClientController.State.Running

            this.port = port;

            WriteTimeout = 33;
            ReadTimeout = Timeout.Infinite;

            _writeBufferSize = writeBufferSize;
            _readBufferSize = readBufferSize;
        }

        public SerialPortCDC(USBC_CDC port) : this(port, 0, 1) { }

        protected override void StartReading()
        {
            ReadTimeout = -1;

            base.StartReading();
        }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            return port.Write(data, offset, length);
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            return port.Read(data, offset, length);
        }

        protected override bool CanWrite { get { return true; } }

        public override void Flush() { }
    }
}
