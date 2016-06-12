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
            try
            {
                await Socket.ConnectAsync(settings.HostName, settings.ServiceName);
            }
            catch (Exception ex)
            {
                throw new Exception("Can't connect to imBMW Bluetooth device. Check that it's paired and online.", ex);
            }

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
                var parser = new MessageParser();
                parser.MessageReceived += parser_MessageReceived;
                while (true)
                {
                    var size = await dataReader.LoadAsync(1028);
                    var data = new byte[size];
                    dataReader.ReadBytes(data);

                    try
                    {
                        parser.Parse(data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Parsing message by SocketClient.");
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

        void parser_MessageReceived(Message message)
        {
            if (message is InternalMessage)
            {
                OnInternalMessageReceived((InternalMessage)message);
            }
            else
            {
                Manager.ProcessMessage(message);
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
