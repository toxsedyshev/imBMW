using System;
using Microsoft.SPOT;
using System.Threading;
using imBMW.Tools;

namespace System.IO.Ports
{
    public class SerialPortEcho : SerialPortBase
    {
        Thread readingThread;
        object bufferSync = new object();
        byte[] buffer;

        protected override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush() { }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            throw new Exception("Not tested");

            lock (bufferSync)
            {
                var t = data.SkipAndTake(offset, length);
                if (buffer == null)
                {
                    buffer = t;
                }
                else
                {
                    buffer.Combine(t);
                }
                if (readingThread != null)
                {
                    readingThread.Resume();
                }
            }
            return length;
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            readingThread = Thread.CurrentThread;
            lock (bufferSync)
            {
                while (buffer == null || buffer.Length == 0)
                {
                    readingThread.Suspend();
                }
                length = Math.Min(length, buffer.Length);
                Array.Copy(buffer, 0, data, offset, length);
                if (buffer.Length == length)
                {
                    buffer = null;
                }
                else
                {
                    buffer = buffer.Skip(length);
                }
                return length;
            }
        }
    }
}
