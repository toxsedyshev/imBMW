using imBMW.Tools;

namespace System.IO.Ports
{
    public class SerialPortHub : SerialPortBase
    {
        readonly ISerialPort[] _ports;
        readonly QueueThreadWorker[] _queues;

        public SerialPortHub(params ISerialPort[] ports)
        {
            _ports = ports;

            _queues = new QueueThreadWorker[ports.Length];

            for (int i = 0; i < ports.Length; i++)
            {
                int index = i;
                ISerialPort port = ports[index];

                _queues[index] = new QueueThreadWorker(o =>
                {
                    var data = (byte[])o;
                    port.Write(data, 0, data.Length);
                });
            }

            for (int i = 0; i < ports.Length; i++)
            {
                int index = i;
                ISerialPort port = ports[index];

                // Read each port and forward data to other ports
                port.DataReceived += (s, e) =>
                {
                    byte[] data = port.ReadAvailable();
                    Write(data, 0, data.Length, index);
                    OnDataReceived(data, data.Length);
                };
            }
        }

        public override void Write(byte[] data, int offset, int length)
        {
            Write(data, offset, length, -1);
        }

        protected void Write(byte[] data, int offset, int length, int except)
        {
            if (except < 0 || except >= _ports.Length)
            {
                except = -1;
            }
            int count = _ports.Length - (except == -1 ? 0 : 1);
            if (count == 0)
            {
                return;
            }
            if (offset != 0 || length != data.Length)
            {
                // Add only required part of data to queue
                data = data.SkipAndTake(offset, length);
            }

            for (int i = 0; i < _ports.Length; i++)
            {
                if (i != except)
                {
                    _queues[i].Enqueue(data);
                }
            }
        }

        /*protected void Write(byte[] data, int offset, int length, ISerialPort except)
        {
            int count = 0;
            ISerialPort[] writePorts;
            if (except == null)
            {
                writePorts = ports;
                count = writePorts.Length;
            }
            else
            {
                writePorts = new ISerialPort[ports.Length];
                foreach (ISerialPort port in ports)
                {
                    if (except != port)
                    {
                        writePorts[count++] = port;
                    }
                }
            }
            if (count < 2)
            {
                if (count == 1)
                {
                    writePorts[0].Write(data, offset, length);
                }
                return;
            }
            Thread[] threads = new Thread[count];
            int i = 0;
            foreach (ISerialPort port in writePorts)
            {
                if (port == null)
                {
                    continue;
                }
                Thread thread = new Thread(() => port.Write(data, offset, length));
                thread.Start();
                threads[i++] = thread;
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }*/

        public override void Flush()
        {
            foreach (ISerialPort port in _ports)
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
