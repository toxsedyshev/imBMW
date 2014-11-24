using imBMW.iBus;
using imBMW.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace imBMW.Clients
{
    public class SocketClient
    {
        public delegate void InternalMessageReceivedHandler(SocketClient sender, InternalMessage message);

        public event InternalMessageReceivedHandler InternalMessageReceived;

        public StreamSocket Socket { get; protected set; }

        public SocketConnectionSettings Settings { get; protected set; }

        public bool AutoReconnect { get; set; }

        DataWriter dataWriter;

        public SocketClient()
        {
            AutoReconnect = true;
        }

        public bool IsConnected
        {
            get
            {
                return dataWriter != null;
            }
        }

        public async Task SendMessage(Message message)
        {
            if (!IsConnected)
            {
                throw new Exception("Not connected.");
            }
            dataWriter.WriteBuffer(message.Packet.AsBuffer());
            await dataWriter.StoreAsync();
            //await dataWriter.FlushAsync();
        }

        public void Disconnect()
        {
            Manager.MessageEnqueued -= Manager_MessageEnqueued;

            if (dataWriter != null)
            {
                dataWriter.DetachStream();
                dataWriter = null;
            }

            lock (this)
            {
                if (Socket != null)
                {
                    Socket.Dispose();
                    Socket = null;
                }
            }
        }

        public virtual async Task Connect(SocketConnectionSettings settings)
        {
            lock (this)
            {
                Socket = new StreamSocket();
            }
            await Socket.ConnectAsync(settings.HostName, settings.ServiceName);

            OnConnected();
        }

        protected void OnConnected()
        {
            dataWriter = new DataWriter(Socket.OutputStream);

            Manager.MessageEnqueued += Manager_MessageEnqueued;

            var dataReader = new DataReader(Socket.InputStream);
            dataReader.InputStreamOptions = InputStreamOptions.Partial;
            ReadingLoop(dataReader);
        }

        private async void ReadingLoop(DataReader dataReader)
        {
            try
            {
                byte[] buffer = null;
                while (true)
                {
                    var size = await dataReader.LoadAsync(1028);
                    var c = new byte[size];
                    dataReader.ReadBytes(c);

                    if (buffer == null)
                    {
                        buffer = c;
                    }
                    else
                    {
                        buffer = buffer.Combine(c);
                    }
                    if (!InternalMessage.CanStartWith(buffer))
                    {
                        if (InternalMessage.CanStartWith(c))
                        {
                            buffer = c;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    var m = InternalMessage.TryCreate(buffer);
                    if (m == null)
                    {
                        continue;
                    }
                    if (m.PacketLength == buffer.Length)
                    {
                        buffer = null;
                    }
                    else
                    {
                        buffer = buffer.Skip(m.PacketLength);
                    }
                    if (m is InternalMessage)
                    {
                        OnInternalMessageReceived((InternalMessage)m);
                    }
                    else
                    {
                        Manager.ProcessMessage(m);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "imBMW socket client reading");
                lock (this)
                {
                    if (Socket != null)
                    {
                        Disconnect();
                        Reconnect();
                    }
                }
            }
        }

        async void Reconnect()
        {
            if (AutoReconnect)
            {
                try
                {
                    await Connect(Settings);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "socket client reconnecting");
                }
            }
        } 

        async void Manager_MessageEnqueued(MessageEventArgs e)
        {
            await SendMessage(e.Message);
        }

        protected void OnInternalMessageReceived(InternalMessage m)
        {
            var e = InternalMessageReceived;
            if (e != null)
            {
                try
                {
                    e(this, m);
                }
                catch(Exception ex)
                {
                    Logger.Error(ex, "while processing incoming internal message");
                }
            }
        }
    }
}
