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
                if (except == port || i == count)
                {
                    continue;
                }
                threads[i] = new Thread(() =>
                {
                    port.Write(data, offset, length);
                });
                threads[i].Start();
                i++;
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            // TODO benchmark in ms
        }

        public override byte[] ReadAvailable(int maxCount)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            foreach (ISerialPort port in ports)
            {
                port.Flush();
            }
        }

        public override event SerialDataReceivedEventHandler DataReceived
        {
            add
            {
                foreach (ISerialPort port in ports)
                {
                    port.DataReceived += value;
                }
            }
            remove
            {
                foreach (ISerialPort port in ports)
                {
                    port.DataReceived -= value;
                }
            }
        }

        protected override bool CanWrite { get { return true; } }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }
    }
}
