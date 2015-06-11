/*
using System;
using Microsoft.SPOT;
using imBMW.Tools;
using System.Threading;

namespace System.IO.Ports
{
    /// <summary>
    /// Provides a connection to a serial communications port that supports line delimited reading and timer-interruptable writing, including timeouts and DataReceived event implementation.
    /// </summary>
    public class SerialTimerInterruptPort : SerialInterruptPortBase, ISerialPort, IDisposable
    {
        /// <summary>
        /// The value of milliseconds to wait after reading before writing.
        /// </summary>
        public int AfterReadDelay = 0;

        /// <summary>
        /// Set to true if writed bytes are echoed (eg. single wire bus) to prevent suspending of writing thread.
        /// </summary>
        public bool IsWriteEchoed = false;

        DateTime LastRead = DateTime.Now;

        Timer resumeTimer;

        /// <summary>
        /// Creates a new instance of SerialInterruptPort class, allowing to specify buffer sizes and blocking input port.
        /// </summary>
        /// <param name="config">An object that contains the configuration information for the serial port.</param>
        /// <param name="afterReadDelay">The value of milliseconds to wait after reading before writing.</param>
        /// <param name="writeBufferSize">The size of output buffer in bytes. Data output is paused for <see cref="AfterWriteDelay"/> milliseconds every time this amount of data is sent. Can be zero to disable pausing.</param>
        /// <param name="readBufferSize">The size of input buffer in bytes. DataReceived event will fire only after this amount of data is received. Default is 1.</param>
        /// <param name="readTimeout">Timeout of port reading.</param>
        public SerialTimerInterruptPort(SerialPortConfiguration config, int afterReadDelay, int writeBufferSize, int readBufferSize, int readTimeout = Timeout.Infinite)
            : base(config, writeBufferSize, readBufferSize, readTimeout)
        {
            AfterReadDelay = afterReadDelay;
        }

        /// <summary>
        /// Creates a new instance of SerialInterruptPort class, with hardware flow control and output pausing disabled. This corresponds to standard <see cref="SerialPort"/> class behavior.
        /// </summary>
        /// <param name="config">An object that contains the configuration information for the serial port.</param>
        /// <param name="afterReadDelay">The value of milliseconds to wait after reading before writing.</param>
        public SerialTimerInterruptPort(SerialPortConfiguration config, int afterReadDelay) : this(config, afterReadDelay, 0, 1) { }

        public override void Dispose()
        {
            DisposeResumeTimer();
            base.Dispose();
        }

        void OnBusyChanged()
        {
            bool busy = !CanWrite;
            OnBusyChanged(busy);
            if (busy)
            {
                DisposeResumeTimer();
                resumeTimer = new Timer((s) =>
                {
                    OnBusyChanged(false);
                    DisposeResumeTimer();
                }, null, AfterReadDelay, 0); 
            }
        }

        void DisposeResumeTimer()
        {
            if (resumeTimer != null)
            {
                resumeTimer.Dispose();
                resumeTimer = null;
            }
        }

        protected override bool CanWrite
        {
            get
            {
                return (DateTime.Now - LastRead).GetTotalMilliseconds() >= AfterReadDelay;
            }
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            int res = base.ReadDirect(data, offset, length);
            if (res > 0 && (!IsWriteEchoed || _writeThread == null))
            {
                LastRead = DateTime.Now;
                OnBusyChanged();
            }
            return res;
        }
    }
}
*/