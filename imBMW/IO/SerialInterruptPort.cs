using System;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace System.IO.Ports
{
    /// <summary>
    /// Provides a connection to a serial communications port that supports line delimited reading and interruptable writing, including timeouts and DataReceived event implementation.
    /// </summary>
    public class SerialInterruptPort : SerialPortBase, ISerialPort, IDisposable  // SerialPort class is marked as sealed, so you can't use this class in components where SerialPort is accepted.
    {                                                                            // We are implementing IDisposable because we have private SerialPort, eventually InterruptPort to dispose.
        /// <summary>
        /// The value of input pin when pauding the data output is requested. Default is true, that is, the input is of active high type.
        /// </summary>
        public bool BusyValue = true;

        protected SerialPort _port;                 // The actual serial port we are wrapping.
        protected InterruptPort _busy;              // An input port which can be used to pause the sending of data.

        /// <summary>
        /// Creates a new instance of SerialInterruptPort class, allowing to specify buffer sizes and blocking input port.
        /// </summary>
        /// <param name="config">An object that contains the configuration information for the serial port.</param>
        /// <param name="busySignal">A <see cref="Cpu.Pin"/> to use as a output hardware flow control. Can be <see cref="Cpu.Pin.GPIO_NONE"/> if none used.</param>
        /// <param name="writeBufferSize">The size of output buffer in bytes. Data output is paused for <see cref="WriteTimeout"/> milliseconds every time this amount of data is sent. Can be zero to disable pausing.</param>
        /// <param name="readBufferSize">The size of input buffer in bytes. DataReceived event will fire only after this amount of data is received. Default is 1.</param>
        /// <param name="readTimeout">Timeout of port reading.</param>
        public SerialInterruptPort(SerialPortConfiguration config, Cpu.Pin busySignal, int writeBufferSize, int readBufferSize, int readTimeout = Timeout.Infinite)
            : base(writeBufferSize, readBufferSize)
        {
            _port = new SerialPort(config.PortName, config.BaudRate, config.Parity, config.DataBits, config.StopBits); // creating the serial port
            
            WriteTimeout = 33;
            ReadTimeout = readTimeout;
            
            _port.Open();

            if (busySignal == Cpu.Pin.GPIO_NONE)        // user does not want to use the output hardware flow control
                _busy = null;
            else
            {                                           // start monitoring the flow control pin for both edges
                _busy = new InterruptPort(busySignal, false, Port.ResistorMode.PullDown, Port.InterruptMode.InterruptEdgeBoth);
                _busy.OnInterrupt += new NativeEventHandler(OnBusyChanged);
            }
        }
        /// <summary>
        /// Creates a new instance of SerialInterruptPort class, with hardware flow control and output pausing disabled. This corresponds to standard <see cref="SerialPort"/> class behavior.
        /// </summary>
        /// <param name="config">An object that contains the configuration information for the serial port.</param>
        public SerialInterruptPort(SerialPortConfiguration config) : this(config, Cpu.Pin.GPIO_NONE, 0, 1) { }

        /// <summary>
        /// Releases resources used by a serial port.
        /// </summary>
        public void Dispose()
        {
            if (_busy != null) _busy.Dispose(); // release the hardware flow control pin, if used
            if (_port != null) _port.Dispose(); // release the serial port if applicable
        }

        // This is event handler for changes on the hardware flow pin.
        private void OnBusyChanged(uint port, uint state, DateTime time)
        {
            OnBusyChanged((state == 1) == BusyValue);

            // currently not writing
            if (_writeThread == null) return;

            if ((state == 1) == BusyValue) _writeThread.Suspend(); // if _busy was set, pause sending the data
            else _writeThread.Resume();                     // if it was cleared, resume sending the data
        }

        protected override bool CanWrite
        {
            get
            {
                return _busy == null || _busy.Read() != BusyValue;
            }
        }

        protected override int WriteDirect(byte[] data, int offset, int lenght)
        {
            return _port.Write(data, offset, lenght);
        }

        /// <summary>
        /// Empties the contents of a serial port's buffer.
        /// </summary>
        public override void Flush()
        {
            _port.Flush();
        }

        public override int ReadTimeout
        {
            get
            {
                return base.ReadTimeout;
            }
            set
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                    _port.ReadTimeout = value;
                    _port.Open();
                }
                else
                {
                    _port.ReadTimeout = value;
                }
                base.ReadTimeout = value;
            }
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            return _port.Read(data, offset, length);
        }

    }
}