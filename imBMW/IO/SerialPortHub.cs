using System;
using Microsoft.SPOT;
using System.Collections;
using System.Threading;

namespace System.IO.Ports
{
    public class SerialPortHub : SerialPortBase, ISerialPort
    {
        ISerialPort[] ports;

        public SerialPortHub(params ISerialPort[] ports)
        {
            this.ports = ports;

            // Read each port and forward data to other ports
            foreach (ISerialPort port in ports)
            {
                port.DataReceived += (s, e) =>
                {
                    byte[] data = port.ReadAvailable();
                    Write(data, 0, data.Length, port);
                    OnDataReceived(data, data.Length);
                };
            }
        }

        public override void Write(byte[] data, int offset, int length)
        {
            Write(data, offset, length, null);
        }

        protected void Write(byte[] data, int offset, int length, ISerialPort except)
        {
            int count = ports.Length;
            if (except != null)
            {
                count--;
            }
            Thread[] threads = new Thread[count];
            int i = 0;
            foreach (ISerialPort port in ports)
            {
                if (except == port)
                {
                    // TODO test it!
                    continue;
                }
                threads[i] = new Thread(() => port.Write(data, offset, length));
                threads[i].Start();
                if (i < count - 1)
                {
                    i++;
                }
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }

        public override void Flush()
        {
            foreach (ISerialPort port in ports)
            {
                port.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override byte[] ReadTo(params byte[] mark)
        {
            throw new NotImplementedException();
        }

        protected override void StartReading()
        {
            // We are reading always
        }

        protected override void StopReading()
        {
            // We are reading always
        }

        protected override bool CanWrite 
        { 
            get { return true; } 
        }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            // This method will not be called
            throw new NotImplementedException();
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            // This method will not be called
            throw new NotImplementedException();
        }
    }
}
