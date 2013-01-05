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
    public class SerialInterruptPort : IDisposable  // SerialPort class is marked as sealed, so you can't use this class in components where SerialPort is accepted.
    {                                               // We are implementing IDisposable because we have private SerialPort, eventually InterruptPort to dispose.
        /// <summary>
        /// Encoding to use when reading or writing string data. Default is UTF-8. Only expanding encodings are supported.
        /// </summary>
        public Encoding Encoding = Encoding.UTF8;
        
        /// <summary>
        /// A string representing the line delimiter in string data.
        /// </summary>
        public string NewLine = "\r\n";

        /// <summary>
        /// Number of milliseconds to pause the writing thread after sending the write buffer.
        /// </summary>
        public int WriteTimeout = 33;

        /// <summary>
        /// Number of milliseconds to wait for data in read methods. Default is <see cref="Timeout.Infinite"/>. Pass zero to make the read methods return immediately when no data are buffered.
        /// </summary>
        public int ReadTimeout = Timeout.Infinite;

        /// <summary>
        /// The value of input pin when pauding the data output is requested. Default is true, that is, the input is of active high type.
        /// </summary>
        public bool BusyValue = true;

        private byte[] _incomingBuffer;             // Circular buffer for incoming data when DataReceived event is being requested.
        private int _incomingBufferPosition;        // Start position in the _incomingBuffer where valid data begins.
        private int _incomingBufferValidLength;     // Number of valid bytes in the _incomingBuffer starting at _incomingBufferPosition.

        protected SerialPort _port;                 // The actual serial port we are wrapping.
        protected InterruptPort _busy;              // An input port which can be used to pause the sending of data.

        private Thread _writeThread;                // Thread which is sending data out. This is usually a calling thread.
        private int _writeBufferSize;               // Size of the output buffer, which can be used to insert pauses between some amount of data.

        private Thread _readThread;                 // Thread which is reading the data when DataReceived event is being requested. We create and dispose this thread inside the class.
        private int _readBufferSize;                // Size of the input buffer. The DataReceived event won't fire until this amount of bytes comes in.

        private AutoResetEvent _readToEvent;        // Handles the thread synchronization when both DataReceived event is being requested and user calls ReadTo().

        /// <summary>
        /// Creates a new instance of SerialInterruptPort class, allowing to specify buffer sizes and blocking input port.
        /// </summary>
        /// <param name="config">An object that contains the configuration information for the serial port.</param>
        /// <param name="busySignal">A <see cref="Cpu.Pin"/> to use as a output hardware flow control. Can be <see cref="Cpu.Pin.GPIO_NONE"/> if none used.</param>
        /// <param name="writeBufferSize">The size of output buffer in bytes. Data output is paused for <see cref="WriteTimeout"/> milliseconds every time this amount of data is sent. Can be zero to disable pausing.</param>
        /// <param name="readBufferSize">The size of input buffer in bytes. DataReceived event will fire only after this amount of data is received. Default is 1.</param>
        public SerialInterruptPort(SerialPortConfiguration config, Cpu.Pin busySignal, int writeBufferSize, int readBufferSize)
        {
            // some initial parameter checks.
            if (writeBufferSize < 0) throw new ArgumentOutOfRangeException("writeBufferSize");
            if (readBufferSize < 1) throw new ArgumentOutOfRangeException("readBuferSize");

            _bufferSync = new object();                 // initializing the sync root object
            _incomingBuffer = new byte[readBufferSize]; // allocating memory for incoming data

            _port = new SerialPort(config.PortName, config.BaudRate, config.Parity, config.DataBits, config.StopBits); // creating the serial port
            _port.Open();
            _writeBufferSize = writeBufferSize;
            _readBufferSize = readBufferSize;

            if (busySignal == Cpu.Pin.GPIO_NONE)        // user does not want to use the output hardware flow control
                _busy = null;
            else
            {                                           // start monitoring the flow control pin for both edges
                _busy = new InterruptPort(busySignal, true, Port.ResistorMode.PullDown, Port.InterruptMode.InterruptEdgeBoth);
//                Debug.Print(_busy.Read()?"busy true":"busy false");
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

        #region Writing
        // This is event handler for changes on the hardware flow pin.
        private void OnBusyChanged(uint port, uint state, DateTime time)
        {
            //Debug.Print((state == 1 ? "busy 1" : "busy 0") + (_busy.Read() ? "true" : "false"));
            // currently not writing
            if (_writeThread == null) return;

            if ((state == 1 ? true : false) == BusyValue) _writeThread.Suspend(); // if _busy was set, pause sending the data
            else _writeThread.Resume();                     // if it was cleared, resume sending the data
        }
        /// <summary>
        /// Writes data to a serial port.
        /// </summary>
        /// <param name="data">The data to write to the serial port.</param>
        /// <remarks>The method does not return until all data are sent, including output buffer pauses and/or hardware flow control pauses, if applicable.</remarks>
        public void Write(params byte[] data)
        {
            Write(data, 0, data.Length);
        }
        /// <summary>
        /// Writes data to a serial port.
        /// </summary>
        /// <param name="data">The input buffer that is to write to the serial port.</param>
        /// <param name="offset">The offset value that indicates where writing from the input buffer to the serial port is to begin.</param>
        /// <param name="length">The number of bytes of data to be written to the serial port.</param>
        /// <remarks>The method does not return until all data is sent, including output buffer pauses and/or hardware flow control pauses, if applicable.</remarks>
        public virtual void Write(byte[] data, int offset, int length)
        {
            _writeThread = Thread.CurrentThread;                                // grab the current thread so that we can pause the writing
            if (_busy != null && _busy.Read() == BusyValue) _writeThread.Suspend();          // do not continue if _busy is already set (eg. the signal was changed when we weren't writing)

            if (_writeBufferSize < 1)                                           // If user does not want to split data into chunks,
            {
                _port.Write(data, 0, data.Length);                              // pass it to the SerialPort output without change.
                return;
            }

            int modulus = length % _writeBufferSize;                            // prepare data which fill the _writeBufferSize completely
            length -= modulus;                                                  // (If there is not enough data to fill it, length would be zero after this line,

            for (int i = offset; i < offset + length; i += _writeBufferSize)    // and this cycle would not execute.)
            {
                _port.Write(data, i, _writeBufferSize);                         // send it out
                Thread.Sleep(WriteTimeout);                                     // and include pause after chunk
            }

            if (modulus > 0)                                                    // If any data left which do not fill whole _writeBuferSize chunk,
            {
                _port.Write(data, offset + length, modulus);                    // send it out as well
                Thread.Sleep(WriteTimeout);                                     // and pause for case consecutive calls to any write method.
            }

            _writeThread = null;                                                // release current thread so that the _busy signal does not affect external code execution
        }
        /// <summary>
        /// Encodes string data using <see cref="Encoding"/> and sends them to a serial port.
        /// </summary>
        /// <param name="text">String data to send.</param>
        /// <remarks>The method does not return until all data are sent, including output buffer pauses and/or hardware flow control pauses, if applicable.</remarks>
        public void Write(string text)
        {
            Write(Encoding.GetBytes(text));
        }
        /// <summary>
        /// Appends <see cref="NewLine"/> to the string, encodes it using <see cref="Encoding"/> and sends it to a serial port.
        /// </summary>
        /// <param name="text">String data to send.</param>
        /// <remarks>The method does not return until all data are sent, including output buffer pauses and/or hardware flow control pauses, if applicable.</remarks>
        public void WriteLine(string text)
        {
            Write(text + NewLine);
        }
        #endregion

        #region Reading
        /// <summary>
        /// Gets number of bytes available in buffer for reading.
        /// </summary>
        public int AvailableBytes { get { return _incomingBufferValidLength; } }
        /// <summary>
        /// Reads all available bytes and removes them from the reading buffer.
        /// </summary>
        /// <returns>An array containing bytes from the reading buffer, or an empty array if there are no data available.</returns>
        public byte[] ReadAvailable()
        {
            return ReadAvailable(int.MaxValue);
        }
        /// <summary>
        /// Reads desired number of bytes and removes them from the reading buffer.
        /// </summary>
        /// <param name="maxCount">The maximum count of bytes to return.</param>
        /// <returns>An array containing bytes from the reading buffer, or an empty array if there are no data available.</returns>
        public byte[] ReadAvailable(int maxCount)
        {
            lock (_bufferSync)
            {
                int count = System.Math.Min(_incomingBufferValidLength, maxCount);  // update the count if there are less data available then requested

                byte[] data = GetBufferedData(count);                               // read the data from buffer
                AdvancePosition(count);                                             // "remove" the data from buffer
                return data;
            }
        }
        /// <summary>
        /// Reads data from a serial port.
        /// </summary>
        /// <param name="buffer">The output buffer that stores the data read from the serial port.</param>
        /// <param name="offset">The offset value that indicates where writing to the output buffer from the serial port is to begin.</param>
        /// <param name="count">The number of bytes of data to be read.</param>
        /// <returns>The number of bytes actually read.</returns>
        public virtual int Read(byte[] buffer, int offset, int count)
        {
            if (IsReading)                                                      // If DataReceived event is being requested,
                lock (_bufferSync)                                              // we have to use our reading buffer:
                {
                    int usedLength = GetBufferedData(buffer, offset, count);    // read data from it
                    AdvancePosition(usedLength);                                // and remove them.
                    return usedLength;                                          // TODO: Implement read timeout.
                }
            else
                return _port.Read(buffer, offset, count);          // Otherwise, we can directly read the serial port data.
        }         
        /// <summary>
        /// Reads data from a serial port up to the <see cref="NewLine"/> value and decodes them as string using <see cref="Encoding"/>.
        /// </summary>
        /// <returns>A string representing received data, if available; null if the read operation times out.</returns>
        /// <remarks>This method does not return until <see cref="NewLine"/> sequence is received.</remarks>
        public string ReadLine()
        {
            byte[] stringData = ReadTo(Encoding.GetBytes(NewLine));
            
            if (stringData == null) return null;                                // ReadTo timed out
            if (stringData.Length < 1) return string.Empty;                     // two consecutive line markers

            return new string(Encoding.GetChars(stringData));                   // fails when data contains invalid bytes for the current Encoding
                                                                                // TODO: Fire an error event with raw data.
        }

        /// <summary>
        /// Reads data from a serial port up the specified byte sequence and removes them from the reading buffer.
        /// </summary>
        /// <param name="mark">The byte sequence which terminates the reading.</param>
        /// <returns>An array with data excluding the specified byte sequence, or null if the operation times out.</returns>
        /// <remarks>This method does not return until the mark sequence is received.</remarks>
        [MethodImpl(MethodImplOptions.Synchronized)] // Do not allow multiple threads to call this method simultaneously, as we have only one _readToEvent. (May be fixed later.)
        protected virtual byte[] ReadTo(params byte[] mark)
        {
            // By using byte[] as a marker instead of char[] or string gives us the advantage of processing "lines" in binary data, however,
            // it definitely limits us to the expanding encondings only. That means, this will not work in the case encoding packs more characters into single byte.
            // We could solve this eg. by params char[] mark override equivalent, but current limitations of decoders (eg. Utf8Decoder class) makes such parsing too complicated nowadays.

            if (mark == null) throw new ArgumentNullException("mark");
            if (mark.Length == 0) throw new ArgumentException("Mark must have non-zero length.");

            if (IsReading)                                     // DataReceived event is being requested so we use internal read buffer rather than the serial port directly
            {
                _readToEvent = new AutoResetEvent(false);      // creates an AutoResetEvent so that the OnDataReceived event handler can signal us that new data are available to check

                Timer readToTimeout = null; bool timedOut = false;
                if (ReadTimeout > 0)
                    readToTimeout = new Timer(delegate { timedOut = true; _readToEvent.Set(); }, null, ReadTimeout, 0);

                while (IsReading)
                {
                    lock (_bufferSync)
                    {
                        int markIndex = BufferIndexOf(mark);                    // look for mark in the received data
                        if (markIndex >= 0)                                     // If found,
                        {
                            byte[] receivedData = GetBufferedData(markIndex);   // read the data up to the mark,
                            AdvancePosition(markIndex + mark.Length);           // and remove them from the buffer.

                            if (readToTimeout != null)                          
                                readToTimeout.Dispose();                        // We are finished, so cancel the timeout timer, if applicable.

                            return receivedData;
                        }
                    }

                    if (ReadTimeout == 0)                                       // If the user does not want to wait and we don't have a line, return null.
                        return null;

                    _readToEvent.WaitOne();                                     // Wait until the OnDataReceived handler signals us there are new data available to check,
                                                                                // or until the timeout timer signals us.
                    if (timedOut)                                               
                        return null;                                            // If it was the timer, return null.
                }

                // Here we are if the DataReceived event was being requested upon calling this method, but all subscribers has detached before any line marker came in.

                _readToEvent = null;                                            // do some cleaning of stuff we don't need for the direct serial port manipulation
                if (timedOut) return null;
                else
                    if (readToTimeout != null)
                        readToTimeout.Dispose();
            }

            byte[] data = new byte[System.Math.Max(_incomingBufferValidLength, _readBufferSize) + mark.Length];
            int offset = GetBufferedData(data, 0, _incomingBufferValidLength); // read any data which left in the internal read buffer

            int markSearchStart = 0;
            while (true)
            {
                if (offset >= data.Length)                                     // If we have filled the buffer, make a bigger one!   
                {                                                              // (the >= is for paranoia reasons, the offset never becomes greater than data.Length in this method)
                    byte[] biggerData = new byte[data.Length * 2];
                    data.CopyTo(biggerData, 0);
                    data = biggerData;
                }

                int read = _port.Read(data, offset, data.Length - offset);                 // read as much data from serial port as fits in our buffer
                if (read < 1)
                    return null;                                                                        // the operation has timed out

                offset += read;                                                                         // offset now points to where next read should start, or in other words valid length 
                int markPos = Array.IndexOf(data, mark[0], markSearchStart, offset - markSearchStart);  // try to find the first byte of mark in the buffer
                if (markPos < 0)
                    markSearchStart = offset;                                                           // we didn't find it, there is no reason to search the whole buffer again next time
                else
                {                                                                                       // okay, we have the first byte
                    if (markPos + mark.Length <= offset)                                                // do we have enough data in the buffer that whole mark could fit in?
                    {
                        int i = 1;                                                                      
                        for (i = 1; i < mark.Length; i++)                                               // if so, check if the next bytes in buffer match the mark bytes
                            if (data[markPos + i] != mark[i]) break;

                        if (i >= mark.Length)
                        {
                            byte[] finalData = new byte[markPos];                                       // if they do, copy data before marker into the new array
                            Array.Copy(data, 0, finalData, 0, markPos);

                            int remains = offset - markPos - mark.Length;
                            if (remains > 0)                                                            // If we grabbed any data we haven't used,
                                lock (_bufferSync)                                                      // push it to the internal read buffer (we are going to return now).
                                {
                                    if (_incomingBuffer.Length < remains)                               // make enough space if the internal buffer is too small to store remaining data
                                        _incomingBuffer = new byte[remains];                            // We have already read all data that where in the buffer before the while loop,
                                                                                                        // so it is okay to lose any current data.
                                    Array.Copy(data, offset - remains, _incomingBuffer, 0, remains);
                                    _incomingBufferPosition = 0;                                        // And so we are storing at the beginning of the circular buffer.
                                    _incomingBufferValidLength = remains;
                                }

                            return finalData;
                        }
                        else                                                                            // If the other bytes do not match the mark bytes, it is not part of the mark, 
                            markSearchStart = markPos + 1;                                              // and start the next search at the next position.
                    }
                    else
                        markSearchStart = markPos;                                                      // We don't know if this is marker or not, so try again this position with more data.
                }
            }
        }
        /// <summary>
        /// Searches the internal circular read buffer for sequence of bytes.
        /// </summary>
        /// <param name="what">The sequence to look for.</param>
        /// <returns>The position of first byte, or -1 if the sequence was not found</returns>
        protected virtual int BufferIndexOf(byte[] what)
        {
            // The same limitations about searching for bytes applies. See ReadTo comments for details.

            int whatLength = what.Length;
            int bufferLength = _incomingBuffer.Length;

            // buffer should not be modified during this method (ie. call in lock(_bufferSync) only)

            if (whatLength > _incomingBufferValidLength) return -1;  // if the desired sequence would not fit into the buffer at all, do not bother searching it

            for (int i = _incomingBufferPosition; i < _incomingBufferPosition + _incomingBufferValidLength; i++)
                if (_incomingBuffer[i % bufferLength] == what[0])
                {                                                                       // we have a first byte match
                    int j;
                    for (j = 1; j < whatLength; j++)
                        if (_incomingBuffer[(i + j) % bufferLength] != what[j]) break;  // check the remaining bytes  

                    if (j >= whatLength)                                                // If the remaining bytes match, 
                        return (i - _incomingBufferPosition) % bufferLength;            // decode the circular position and return it;
                }                                                                       // else try the next byte.

            return -1;
        }
        #endregion

        /// <summary>
        /// Empties the contents of a serial port's buffer.
        /// </summary>
        public void Flush()
        {
            _port.Flush();
        }

        #region DataReceived event stuff
        private object _bufferSync;                 // Sync root object for manipulation with the _incomingBuffer and/or its position/valid length fields.
        private bool _continueReading;              // A soft way to end the reading thread.
        
        // The main loop of reading thread. This uses the blocking SerialPort.Read() method to monitor the serial port and fires the DataReceived event.
        private void ReadLoop()
        {
            byte[] buffer = new byte[_readBufferSize];
            int read;
            while (_continueReading)
            {
                try { read = _port.Read(buffer, 0, _readBufferSize); }    // wait for some data (set _readBufferSize to 1 to wait for any data)
                catch (ThreadAbortException) { return; }                                    // (if we were aborted, pass away silently)
                OnDataReceived(buffer, read);                                               // and process it
            }
        }

        /// <summary>
        /// Adds the received data into internal circular read buffer and fires DataReceived event.
        /// </summary>
        /// <param name="data">An array with data received.</param>
        /// <param name="validLength">Number of valid bytes in the array.</param>
        protected virtual void OnDataReceived(byte[] data, int validLength)
        {         
            // See GetBufferedData(byte[], int, int) for the illustration of circular buffer.

            lock (_bufferSync)
            {
                if (_incomingBufferValidLength + validLength > _incomingBuffer.Length)      // If the received data would not fit in the internal buffer,
                {
                    _incomingBuffer = GetBufferedData(_incomingBuffer.Length * 2);          // make it bigger and align the current data at the buffer beginning.
                    _incomingBufferPosition = 0;
                }

                int start1 = (_incomingBufferPosition + _incomingBufferValidLength) % _incomingBuffer.Length;   // where the first phase of copy should start (start2 = 0)
                int end1 = start1 + validLength;                                                                // virtual copy end
                int end2 = 0;

                if (end1 > _incomingBuffer.Length)                                                              // if the end is actually wrapped in the circular buffer
                {
                    end2 = end1 % _incomingBuffer.Length;                                                       // move the overlapping part into the second phase of copy
                    end1 = _incomingBuffer.Length;
                }

                Array.Copy(data, 0, _incomingBuffer, start1, end1 - start1);                                    // first phase of copy (to the middle of the buffer up to its end)
                Array.Copy(data, end1 - start1, _incomingBuffer, 0, end2);                                      // second one          (to the beginning of the buffer up to the wrapped end)
                _incomingBufferValidLength += validLength;
            }

            if (_readToEvent != null)
                _readToEvent.Set();

            if (_dataReceivedHandlers != null)
                _dataReceivedHandlers(this, null);
        }

        /// <summary>
        /// Returns specified amount of data from internal circular read buffer. If there is not enough data available, remaining bytes are filled with zeros.
        /// </summary>
        /// <param name="arraySize">The number of bytes to return.</param>
        /// <returns>An array of size arraySize, filled with data from internal read buffer, if available.</returns>
        protected byte[] GetBufferedData(int arraySize)
        {
            byte[] data = new byte[arraySize];          // This method can be (and is) used to increase the internal buffer size,
            GetBufferedData(data, 0, arraySize);        // with the side effect of aligning the circular wrapped data linearly from the beginning.
            return data;
        }
        /// <summary>
        /// Copies specified amount of data from internal circular read buffer to the specified linear buffer.
        /// </summary>
        /// <param name="buffer">The linear buffer that stores the data read from the internal read buffer.</param>
        /// <param name="offset">The offset value that indicates where writing to the linear buffer from the internal read buffer is to begin.</param>
        /// <param name="count">The number of bytes of data to be copied.</param>
        /// <returns>The number of bytes actually copied.</returns>
        protected virtual int GetBufferedData(byte[] buffer, int offset, int count)
        {
            //                                0  1  2  3  4  5  6  7  8  9 10 11 12              (13) (14)
            // example of _internalBufer:   [ E  F  x  x  x  x  x  x  x  A  B  C  D ]            ( E) ( F)
            //                              _internalBufferStartPosition = 9
            //                              _internalBufferValidLength   = 6  
            // copy 1st phase:               start1 = 9, end1 = 13
            // copy 2nd phase:              (start2 = 0) len2 = 2                  (virtual copy end = 15)

            count = System.Math.Min(_incomingBufferValidLength, count);
            if (count < 1) return 0;

            int end1 = _incomingBufferPosition + count;    // virtual copy end
            int len2 = 0;

            if (end1 > _incomingBuffer.Length)                                                                  // if the end is actually wrapped in the circular buffer
            {
                len2 = end1 % _incomingBuffer.Length;                                                           // move the overlapping part into the second phase of copy
                end1 = _incomingBuffer.Length;
            }

            Array.Copy(_incomingBuffer, _incomingBufferPosition, buffer, 0, end1 - _incomingBufferPosition);    // first phase of copy (from middle of the buffer to the end)
            Array.Copy(_incomingBuffer, 0, buffer, end1 - _incomingBufferPosition, len2);                       // second one          (from beginning of the buffer to the wrapped end)

            return count;
        }
        /// <summary>
        /// Advances the internal circular read bufer position by specified amount and updates the valid length field accordingly.
        /// </summary>
        /// <param name="count">The number of bytes to move forward.</param>
        protected virtual void AdvancePosition(int count)
        {
            _incomingBufferPosition = (_incomingBufferPosition + count) % _incomingBuffer.Length;   // increase the pointer and wrap it if needed
            _incomingBufferValidLength -= count;                                                    // keep the virtual end at the same position
        }

        // Backing field for the DataReceived event. This is usually auto generated by compiler, but here we need a custom behavior:
        // We start waiting for the data when anybody subscribes to the event, and we cancel the waiting when no one is longer interested.
        private SerialDataReceivedEventHandler _dataReceivedHandlers;
        /// <summary>
        /// Represents the method that will handle the data received event of a <see cref="SerialInterruptPort"></see> object.
        /// </summary>
        public event SerialDataReceivedEventHandler DataReceived
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                SerialDataReceivedEventHandler oldHandlers = _dataReceivedHandlers;
                SerialDataReceivedEventHandler newHandlers = (SerialDataReceivedEventHandler)Delegate.Combine(oldHandlers, value);  // add a new handler
                try
                {
                    _dataReceivedHandlers = newHandlers;
                    if (newHandlers != null && oldHandlers == null)                             // if this is first subscription
                        StartReading();                                                         // start the ReadLoop
                }
                catch                                                                           // invocation list update failed
                {
                    _dataReceivedHandlers = oldHandlers;                                        // restore last successful invocation list
                    if (oldHandlers == null)                                                    // if this means no subcription, stop the ReadLoop
                        StopReading();
                    throw;                                                                      // and let the user know
                }
            }
            
            [MethodImpl(MethodImplOptions.Synchronized)]
            remove
            {
                SerialDataReceivedEventHandler oldHandlers = _dataReceivedHandlers;
                SerialDataReceivedEventHandler newHandlers = (SerialDataReceivedEventHandler)Delegate.Remove(oldHandlers, value);   // remove a handler
                try
                {
                    _dataReceivedHandlers = newHandlers;
                    if (newHandlers == null && oldHandlers != null)                             // if we removed last subscription,
                        StopReading();                                                          // stop the ReadLoop
                }
                catch                                                                           // invocation list update failed
                {
                    _dataReceivedHandlers = oldHandlers;                                        // restore last successful invocation list
                    throw;                                                                      // and let the user know
                }
            }
        }

        /// <summary>
        /// Stops waiting for data.
        /// </summary>
        protected virtual void StopReading()
        {
            _continueReading = false; // If the ReadLoop is not in the _port.Read() method, the soft way will work.
            
            if (_writeThread != null && _readThread.ThreadState == ThreadState.WaitSleepJoin) // otherwise,
                _readThread.Abort();                                                          // take a hammer (we need to end the thread, otherwise it would steal the next data coming)

            if (_readToEvent != null)                                                         // if ReadTo() method is being called,
                _readToEvent.Set();                                                           // let it know we have some new data to check
        }
        /// <summary>
        /// Starts waiting for data in order to fire DataReceived event.
        /// </summary>
        protected virtual void StartReading()
        {
            _continueReading = true;

            if (!IsReading)
            {
                _readThread = new Thread(ReadLoop);
                _readThread.Start();
            }
        }
        /// <summary>
        /// Gets whether the waiting for data is active.
        /// </summary>
        protected virtual bool IsReading
        {
            get { return _readThread != null && (_readThread.ThreadState == ThreadState.WaitSleepJoin || _readThread.ThreadState == ThreadState.Running); }
        }
        #endregion
    }
}