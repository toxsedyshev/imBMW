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
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }

        public ConnectionState State { get; protected set; } = ConnectionState.Disconnected;

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
                return State == ConnectionState.Connected;
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
            lock (this)
            {
                if (State != ConnectionState.Disconnected)
                {
                    return;
                }
                Logger.Info("imBMW client disconnected");
                Manager.MessageEnqueued -= Manager_MessageEnqueued;

                if (dataWriter != null)
                {
                    dataWriter.DetachStream();
                    dataWriter = null;
                }

                if (Socket != null)
                {
                    Socket.Dispose();
                    Socket = null;
                }
                State = ConnectionState.Disconnected;
            }
        }

        public virtual async Task Connect(SocketConnectionSettings settings)
        {
            Disconnect();
            lock (this)
            {
                State = ConnectionState.Connecting;
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
            State = ConnectionState.Connected;
            ReadingLoop(dataReader);
        }

        private async void ReadingLoop(DataReader dataReader)
        {
            try
            {
                Logger.Info("imBMW client connected");
                var parser = new MessageParser();
                parser.MessageReceived += parser_MessageReceived;
                while (IsConnected)
                {
                    var size = await dataReader.LoadAsync(1028);
                    var data = new byte[size];
                    if (!IsConnected)
                    {
                        return;
                    }
                    dataReader.ReadBytes(data);

                    try
                    {
                        parser.Parse(data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Parsing message by client.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!IsConnected)
                {
                    return;
                }
                Logger.Error(ex, "imBMW client reading");
                lock (this)
                {
                    if (Socket != null)
                    {
                        OnBeforeDisconnect();
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

        async void OnBeforeDisconnect()
        {
            if (AutoReconnect)
            {
                try
                {
                    Logger.Info("imBMW client reconnecting..");
                    await Connect(Settings);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "imBMW client reconnecting");
                }
            }
            else
            {
                Disconnect();
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
