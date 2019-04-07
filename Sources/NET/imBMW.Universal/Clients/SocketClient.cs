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
using System.Threading;

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

        public event Action Connecting;

        public event Action Connected;

        public event Action Disconnected;

        public delegate void InternalMessageReceivedHandler(SocketClient sender, InternalMessage message);

        public event InternalMessageReceivedHandler InternalMessageReceived;

        protected StreamSocket Socket { get; set; }

        private ConnectionState state = ConnectionState.Disconnected;

        public SocketConnectionSettings Settings { get; protected set; }

        public bool AutoReconnect { get; set; } = true;

        private DataWriter dataWriter;
        private DataReader dataReader;
        private CancellationTokenSource cancellationToken;
        private Task readingLoopTask;

        public SocketClient()
        { }

        public SocketClient(SocketConnectionSettings settings)
        {
            Settings = settings;
        }

        public bool IsConnected
        {
            get
            {
                return State == ConnectionState.Connected 
                    && cancellationToken != null
                    && !cancellationToken.IsCancellationRequested;
            }
        }

        public ConnectionState State
        {
            get => state;
            protected set
            {
                if (state == value)
                {
                    return;
                }
                state = value;
                try
                {
                    switch (value)
                    {
                        case ConnectionState.Connecting:
                            Connecting?.Invoke();
                            break;
                        case ConnectionState.Connected:
                            Connected?.Invoke();
                            break;
                        case ConnectionState.Disconnected:
                            Disconnected?.Invoke();
                            break;
                    }
                }
                catch { }
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
                if (State == ConnectionState.Disconnected)
                {
                    return;
                }
                Manager.MessageEnqueued -= Manager_MessageEnqueued;

                try
                {
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

                    if (cancellationToken != null)
                    {
                        cancellationToken.Cancel();
                        cancellationToken = null;
                    }
                }
                catch
                {
                    dataWriter = null;
                    Socket = null;
                    cancellationToken = null;
                }
                OnDisconnected();
            }
        }

        protected virtual void OnDisconnected()
        {
            State = ConnectionState.Disconnected;
        }

        public virtual async Task Connect()
        {
            if (Settings == null)
            {
                throw new Exception("Settings are not set.");
            }
            await Connect(Settings);
        }

        public async Task Connect(SocketConnectionSettings settings)
        { 
            try
            {
                lock (this)
                {
                    if (!(this is BluetoothClient))
                    {
                        CheckAlreadyConnected();
                    }
                    State = ConnectionState.Connecting;
                    Socket = new StreamSocket();
                }

                await Socket.ConnectAsync(settings.HostName, settings.ServiceName);

                lock (this)
                {
                    Settings = settings;
                    OnConnected();
                }
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        protected void CheckAlreadyConnected()
        {
            if (State != ConnectionState.Disconnected)
            {
                throw new Exception("Client is already connected or connecting.");
            }
        }

        protected void OnConnected()
        {
            dataWriter = new DataWriter(Socket.OutputStream);

            Manager.MessageEnqueued -= Manager_MessageEnqueued;
            Manager.MessageEnqueued += Manager_MessageEnqueued;

            dataReader = new DataReader(Socket.InputStream);
            dataReader.InputStreamOptions = InputStreamOptions.Partial;
            State = ConnectionState.Connected;
            cancellationToken = new CancellationTokenSource();
            readingLoopTask = Task.Factory.StartNew(ReadingLoop, cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async void ReadingLoop()
        {
            try
            {
                var parser = new MessageParser();
                parser.MessageReceived += parser_MessageReceived;
                while (IsConnected)
                {
                    var size = await dataReader.LoadAsync(1028);
                    if (size == 0)
                    {
                        OnBeforeDisconnect();
                        return;
                    }
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
                        Logger.Error(ex, "Parsing message by client");
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
            finally
            {
                Logger.Info("imBMW client reading loop ended");
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
            Disconnect();
            if (AutoReconnect)
            {
                try
                {
                    Logger.Info("imBMW client reconnecting..");
                    await Connect();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "imBMW client reconnecting");
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
