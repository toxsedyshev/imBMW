#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using System.Text;
using imBMW.Tools;
using imBMW.Features.Localizations;
using imBMW.Features.Menu;
using imBMW.Multimedia.Models;
using System.Threading;
using imBMW.iBus;

namespace imBMW.Multimedia
{
    public class BluetoothWT32 : AudioPlayerBase
    {
        SerialPortBase port;
        QueueThreadWorker queue;
        MenuScreen menu;
        byte[] btBuffer;
        int btBufferLen;
        int btBufferMuxLen = 500;
        string lastControlCommand = "";
        string connectedAddress;
        string lastConnectedAddress;
        bool isHFPConnected;
        bool isMuxMode;
        int initStep = 0;
        string pin;

        //bool isSleeping;

        static string[] nowPlayingTags = new string[] { "TITLE", "ARTIST", "ALBUM", "GENRE", "TRACK_NUMBER", "TOTAL_TRACK_NUMBER", "PLAYING_TIME" };
        
        public BluetoothWT32(string port, string pin = "0000")
        {
            Name = "Bluetooth";

            this.pin = pin;

            SPPLink = Link.Unset;

            queue = new QueueThreadWorker(ProcessSendCommand);

            this.port = new SerialInterruptPort(new SerialPortConfiguration(port, BaudRate.Baudrate115200, Parity.None, 8, StopBits.One, true), Cpu.Pin.GPIO_NONE, 0, 60, 0);
            this.port.NewLine = "\n";
            this.port.DataReceived += port_DataReceived;

            BTCommandReceived += (s, link, data) =>
            {
                if (link == Link.Control) { ProcessBTNotification(Encoding.UTF8.GetString(data)); }
            };

            IsMuxMode = true;
            //Thread.Sleep(1000); IsMuxMode = false; throw new Exception("WOW");
            SendCommand("RESET");
        }

        public bool NowPlayingTagsSeparatedRows { get; set; }

        public Link SPPLink { get; set; }

        protected bool IsMuxMode
        {
            get
            {
                return isMuxMode;
            }
            set
            {
                if (isMuxMode == value)
                {
                    return;
                }
                SendCommand("SET CONTROL MUX " + (value ? 1 : 0));
                isMuxMode = value;
                btBuffer = null;
                btBufferLen = 0;
            }
        }

        public string ConnectedAddress
        {
            get { return connectedAddress; }
            set
            {
                if (connectedAddress == value)
                {
                    return;
                }
                if (value == null)
                {
                    IsPlaying = false;
                }
                connectedAddress = value;
                if (value == null)
                {
                    OnStatusChanged(Localization.Current.Disconnected, PlayerEvent.Wireless);
                }
                else
                {
                    lastConnectedAddress = value;
                    OnStatusChanged(Localization.Current.Connected, PlayerEvent.Wireless);
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return ConnectedAddress != null;
            }
            protected set
            {
                if (value)
                {
                    throw new Exception("Set ConnectedAddress instead.");
                }
                else
                {
                    ConnectedAddress = null;
                    IsPlaying = false;
                    IsHFPConnected = false;
                }
            }
        }

        public bool IsHFPConnected
        {
            get { return isHFPConnected; }
            set { isHFPConnected = value; }
        }

        public delegate void BTCommandHandler(BluetoothWT32 sender, Link link, byte[] command);

        public event BTCommandHandler BTCommandReceived;

        protected void OnBTCommandReceived(Link link, byte[] command)
        {
            var e = BTCommandReceived;
            if (e != null)
            {
                try
                {
                    e(this, link, command);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "while processing WT32 incoming command");
                }
            }
        }

        public enum Link : byte 
        {
            Unset = 0xEF,
            Control = 0xFF,
            One = 0x00,
            Two = 0x01,
            Three = 0x02,
            Four = 0x03,
            Five = 0x04,
            Six = 0x05,
            Seven =  0x06
        }

        class MuxCommand
        {
            public Link Link { get; protected set; }

            public string Command { get; protected set; }

            public byte[] Data { get; protected set; }

            public bool IsStringCommand { get; protected set; }

            public MuxCommand(string command, Link link)
            {
                if (link == Link.Unset)
                {
                    throw new Exception("Can't send to Link.Unset.");
                }
                IsStringCommand = true;
                Link = link;
                Command = command;
            }

            public MuxCommand(byte[] command, Link link, string description = "")
                : this(description, link)
            {
                IsStringCommand = false;
                Data = command;
            }

            public byte[] GetBytes()
            {
                if (IsStringCommand)
                {
                    return Encoding.UTF8.GetBytes(Command);
                }
                else
                {
                    return Data;
                }
            }
        }

        public void SendCommand(string command, Link link = Link.Control)
        {
            /*if (isSleeping)
            {
                isSleeping = false;
                SendCommand(" AT"); // wakes up after SLEEP 
            }*/
            if (link == Link.Control)
            {
                lastControlCommand = command;
            }
            object cmd;
            if (!IsMuxMode)
            {
                cmd = command;
            }
            else
            {
                cmd = new MuxCommand(command, link);
            }
            queue.Enqueue(cmd);
        }

        public void SendCommand(Message message, Link link, string description = null)
        {
            if (description == null)
            {
                description = message.ToString();
            }
            SendCommand(message.Packet, link, description);
        }

        public void SendCommand(byte[] command, Link link, string description = "")
        {
            if (command.Length > 1023)
            {
                throw new Exception("WT32 command length limit is 10 bytes. Can't send: " + description);
            }
            var cmd = new MuxCommand(command, link, description);
            queue.Enqueue(cmd);
        }

        void ProcessSendCommand(object o)
        {
            if (o is string)
            {
                var s = (string)o;
                Logger.Info(s, "> BT");
                port.WriteLine(s);
            }
            else
            {
                var cmd = (MuxCommand)o;
                var bytes = cmd.GetBytes();
                var len = bytes.Length;
                if (len > 1023)
                {
                    throw new Exception("WT32 command length limit is 10 bits. Can't send: " + cmd.Command);
                }
                var buf = new byte[len + 5];

                int pos = 0;
                buf[pos++] = 0xBF;  // SOF 
                buf[pos++] = (byte)cmd.Link;
                buf[pos++] = (byte)(len >> 8);  // Flags (reserved) 6 bits + len 2 bits (256-1023)
                buf[pos++] = (byte)len;         //                         + len 8 bits (0-255)
                Array.Copy(bytes, 0, buf, pos, len);
                pos += len;
                buf[pos++] = (byte)(((byte)cmd.Link) ^ 0xFF); // nlink

                Logger.Info("MUX " + (cmd.Link == Link.Control ? "CTRL" : cmd.Link.ToString()) + ": " + cmd.Command, "> BT");
                port.Write(buf);
            }
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.AvailableBytes == 0)
            {
                return;
            }
            var data = port.ReadAvailable();
            var mux = IsMuxMode;
            if (mux)
            {
                if (btBuffer == null || btBuffer.Length != btBufferMuxLen)
                {
                    btBuffer = new byte[btBufferMuxLen];
                    btBufferLen = 0;
                }
                if (btBuffer.Length - btBufferLen < data.Length)
                {
                    btBufferMuxLen *= 2;
                    Logger.Info("Extending buffer to " + btBufferMuxLen, "BT");
                    var newBuf = new byte[btBufferMuxLen];
                    if (btBufferLen > 0)
                    {
                        Array.Copy(btBuffer, newBuf, btBufferLen);
                    }
                    btBuffer = newBuf;
                }
                Array.Copy(data, 0, btBuffer, btBufferLen, data.Length);
                btBufferLen += data.Length;
            }
            else
            {
                if (btBuffer == null || btBuffer.Length == btBufferMuxLen)
                {
                    btBuffer = data;
                }
                else
                {
                    btBuffer = btBuffer.Combine(data);
                }
            }
            int index = -1;
            int bLen;
            if (mux)
            {
                while ((index = btBuffer.IndexOf(0xBF, index + 1, btBufferLen)) != -1)
                {
                    bLen = btBufferLen - index;
                    if (bLen < 5)
                    {
                        break;
                    }
                    var link = btBuffer[index + 1];
                    if (link != (byte)Link.Control && link > (byte)Link.Seven)
                    {
                        continue;
                    }
                    var dataLen = ((btBuffer[index + 2] & 0x11) << 8) + btBuffer[index + 3]; // 10-byte length
                    var packLen = dataLen + 5;
                    if (bLen < packLen)
                    {
                        break;
                    }
                    if (btBuffer[index + packLen - 1] != (link ^ 0xFF))
                    {
                        continue;
                    }
                    OnBTCommandReceived((Link)link, btBuffer.SkipAndTake(index + 4, dataLen));
                    if (bLen != packLen)
                    {
                        Array.Copy(btBuffer, index + packLen, btBuffer, 0, bLen - packLen);
                    }
                    btBufferLen = bLen - packLen;
                    index = -1;
                }
                if (index > 0)
                {
                    #if DEBUG
                    Logger.Warning("Skipping BT data: " + ASCIIEncoding.GetString(btBuffer, 0, btBufferLen - index));
                    #endif
                    Array.Copy(btBuffer, index, btBuffer, 0, btBufferLen - index);
                    btBufferLen -= index;
                }
            }
            else
            {
                while (btBuffer != null && ((index = btBuffer.IndexOf(0x0D)) != -1 || (index = btBuffer.IndexOf(0x0A)) != -1))
                {
                    if (index != 0)
                    {
                        try
                        {
                            OnBTCommandReceived(Link.Control, btBuffer.SkipAndTake(0, index));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "while parsing WT32 message in command mode");
                        }
                    }
                    var skip = btBuffer[index] == 0x0A ? 1 : 2;
                    if (index + skip >= btBuffer.Length)
                    {
                        btBuffer = null;
                    }
                    else
                    {
                        btBuffer = btBuffer.Skip(index + skip);
                    }
                }
            }
        }

        private void ProcessBTNotification(string s)
        {
            if (s.IndexOf('\r') >= 0)
            {
                var messages = s.Split('\r', '\n');
                foreach (var m in messages)
                {
                    if (m.Length > 0)
                    {
                        ProcessBTNotification(m);
                    }
                }
                return;
            }
            s = s.Trim();
            if (s.Length == 0)
            {
                return;
            }
            Logger.Info(s, "BT <");
            var p = s.IndexOf(' ') > 0 ? s.Split(' ') : null;
            if (p != null)
            {
                s = p[0];
            }
            var plen = p == null ? 0 : p.Length;
            switch (s)
            {
                case "READY.":
                    switch (initStep)
                    {
                        case 0:
                            // init
                            //SendCommand("SET");
                            SendCommand("SET PROFILE SPP imBMW");
                            SendCommand("SET PROFILE A2DP SINK");
                            SendCommand("SET PROFILE AVRCP CONTROLLER");
                            SendCommand("SET PROFILE HFP ON");
                            SendCommand("SET BT PAGEMODE 3 2000 1");
                            SendCommand("SET BT CLASS 240408");
                            SendCommand("SET BT SSP 3 0");
                            SendCommand("SET BT AUTH * " + pin);
                            SendCommand("SET BT NAME imBMW");
                            SendCommand("SET CONTROL MICBIAS b a");
                            SendCommand("SET CONTROL GAIN 8 8 DEFAULT");
                            SendCommand("SET CONTROL PREAMP 1 0");
                            SendCommand("RESET");
                            break;
                        default:
                            // inited
                            SendCommand("VOLUME 8");
                            //SendCommand("SET CONTROL MICBIAS b 10");
                            SendCommand("SET");
                            //SendCommand("RFCOMM CREATE");
                            //Connect();
                            break;
                    }
                    initStep++;
                    break;
                case "RING":
                    if (plen == 5)
                    {
                        switch (p[4])
                        {
                            case "A2DP":
                                OnA2DPConnected(p[2], p[1]);
                                break;
                            case "AVRCP":
                                OnAVRCPConnected(p[2], p[1]);
                                break;
                            case "RFCOMM":
                                if (p[3] == "1")
                                {
                                    SPPLink = (Link)byte.Parse(p[1]);
                                }
                                break;
                        }
                    }
                    break;
                case "CONNECT":
                    if (plen == 4)
                    {
                        switch (p[2])
                        {
                            case "A2DP":
                                OnA2DPConnected(lastConnectedAddress, p[1]);
                                break;
                            case "AVRCP":
                                OnAVRCPConnected(lastConnectedAddress, p[1]);
                                break;
                            case "RFCOMM":
                                break;
                        }
                    }
                    break;
                case "AVRCP":
                    if (plen > 5 && p[1] == "GET_ELEMENT_ATTRIBUTES_RSP")
                    {
                        ParseNowPlaying(p);
                    }
                    else if (plen > 3 && p[1] == "REGISTER_NOTIFICATION_RSP")
                    {
                        switch (p[3])
                        {
                            case "PLAYBACK_STATUS_CHANGED":
                                IsPlaying = p[4] == "PLAYING";
                                if (p[2] == "CHANGED")
                                {
                                    SendCommand("AVRCP PDU 31 1"); // resubscribe
                                }
                                break;
                            case "TRACK_CHANGED":
                                // [4] and [5] = 0 and 0 ?
                                if (p[2] == "CHANGED")
                                {
                                    SendCommand("AVRCP PDU 20 0"); // get track info
                                    SendCommand("AVRCP PDU 31 2"); // resubscribe
                                }
                                break;
                            case "NOW_PLAYING_CHANGED":
                                if (p[2] == "CHANGED")
                                {
                                    SendCommand("AVRCP PDU 31 9"); // resubscribe
                                }
                                break;
                        }
                    }
                    break;
                case "NO":
                    if (plen > 2 && p[1] == "CARRIER")
                    {
                        if (p[2] == "0")
                        {
                            IsConnected = false;
                        }
                        if (p[2] == ((byte)SPPLink).ToString())
                        {
                            SPPLink = Link.Unset;
                        }
                    }
                    break;
                case "VOLUME":
                    if (plen > 1)
                    {
                        OnStatusChanged((int)(100 * int.Parse(p[1]) / 15) + "%", PlayerEvent.Settings);
                    }
                    break;
            }
        }

        public void SetMicGain(byte b)
        {
            SendCommand("SET CONTROL GAIN " + b.ToHex() + " 8 DEFAULT");
            SendCommand("SET");
        }

        void ParseNowPlaying(string[] p)
        {
            var i = 0;
            string tag = null;
            string value = null;
            var n = new TrackInfo();
            foreach (var s in p)
            {
                var isLast = i == p.Length - 1;
                var isTag = nowPlayingTags.Contains(s) && (value == null || value.Length > 0 && value[value.Length - 1] == '"');
                if (tag != null && !isTag)
                {
                    value = (value == null) ? s : value + " " + s;
                }
                if (isLast || isTag)
                {
                    if (tag != null && value.Length > 2)
                    {
                        value = value.Substring(1, value.Length - 2);
                        switch (tag)
                        {
                            case "TITLE":
                                n.Title = value;
                                break;
                            case "ARTIST":
                                n.Artist = value;
                                break;
                            case "ALBUM":
                                n.Album = value;
                                break;
                            case "GENRE":
                                n.Genre = value;
                                break;
                            case "TRACK_NUMBER":
                                try { n.TrackNumber = int.Parse(value); }
                                catch { }
                                break;
                            case "TOTAL_TRACK_NUMBER":
                                try { n.TotalTracks = int.Parse(value); }
                                catch { }
                                break;
                            case "PLAYING_TIME":
                                try { n.TrackLength = int.Parse(value); }
                                catch { }
                                break;
                        }
                    }
                    if (isTag)
                    {
                        tag = s;
                        value = null;
                    }
                }
                i++;
            }
            NowPlaying = n;
        }

        void OnAVRCPConnected(string address, string link)
        {
            if (IsConnected && address != ConnectedAddress)
            {
                SendCommand("CLOSE " + link);
                return;
            }
            SendCommand("AVRCP PDU 20 0"); // get now playing
            //SendCommand("AVRCP PDU 10 3"); // get supported events
            SendCommand("AVRCP PDU 31 1"); // subscribe..
            SendCommand("AVRCP PDU 31 2"); // ..
            SendCommand("AVRCP PDU 31 9"); // ..
            // TODO set max volume
            Play();
        }

        void OnA2DPConnected(string address, string link)
        {
            if (IsConnected && address != ConnectedAddress)
            {
                SendCommand("CLOSE " + link);
                return;
            }
            ConnectedAddress = address;
            if (PlayerHostState == Multimedia.PlayerHostState.Off)
            {
                Disconnect();
                return;
            }
            if (link == "1") // TODO check count of a2dp and avrcp connections
            {
                SendCommand("CALL " + address + " 17 AVRCP");
            }
        }

        public void Connect()
        {
            if (!IsConnected)
            {
                if (lastConnectedAddress != null)
                {
                    SendCommand("CALL " + lastConnectedAddress + " 19 A2DP");
                }
                else
                {
                    //SendCommand("INQUIRY 4");
                }
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                // SendCommand("CLOSE 2"); // TODO test all 3 connections are closed by closing 0
                SendCommand("CLOSE 0");
                //SendCommand("DELAY 0 1000 SLEEP"); // commented: can't wake up via uart after sleep
                //isSleeping = true;
            }
        }

        public override void Next()
        {
            if (IsConnected)
            {
                SendCommand("AVRCP FORWARD");
            }
        }

        public override void Prev()
        {
            if (IsConnected)
            {
                SendCommand("AVRCP BACKWARD");
            }
        }

        public override void VoiceButtonPress()
        {
            throw new NotImplementedException();
        }

        public override void VoiceButtonLongPress()
        {
            throw new NotImplementedException();
        }

        public override bool RandomToggle()
        {
            throw new NotImplementedException();
        }

        public override void VolumeUp()
        {
            if (IsConnected)
            {
                SendCommand("AVRCP UP");
            }
        }

        public override void VolumeDown()
        {
            if (IsConnected)
            {
                SendCommand("AVRCP DN");
            }
        }

        public override Features.Menu.MenuScreen Menu
        {
            get
            {
                if (menu == null)
                {
                    menu = new MenuScreen(Name);

                    var settingsScreen = new MenuScreen(s => Localization.Current.Settings);
                    settingsScreen.Status = Name;
                    settingsScreen.AddItem(new MenuItem(i => IsConnected ? Localization.Current.Disconnect : Localization.Current.Connect, i => { if (IsConnected) Disconnect(); else Connect(); }));
                    settingsScreen.AddItem(new MenuItem(i => "BT " + Localization.Current.Volume + " +", i => SendCommand("VOLUME up")));
                    settingsScreen.AddItem(new MenuItem(i => "BT " + Localization.Current.Volume + " -", i => SendCommand("VOLUME down")));
                    settingsScreen.AddItem(new MenuItem(i => Localization.Current.Volume + " +", i => VolumeUp()));
                    settingsScreen.AddItem(new MenuItem(i => Localization.Current.Volume + " -", i => VolumeDown()));
                    settingsScreen.AddBackButton();

                    var nowPlayingScreen = new MenuScreen(s => StringHelpers.IsNullOrEmpty(menu.Status) ? Localization.Current.Disconnected : menu.Status);
                    NowPlayingChanged += (p, n) => UpdateNowPlayingScreen(nowPlayingScreen, n);
                    UpdateNowPlayingScreen(nowPlayingScreen, NowPlaying);

                    menu.AddItem(new MenuItem(i => IsPlaying ? Localization.Current.Pause : Localization.Current.Play, i => PlayPauseToggle()));
                    menu.AddItem(new MenuItem(i => Localization.Current.NowPlaying, MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = nowPlayingScreen });
                    menu.AddItem(new MenuItem(i => Localization.Current.NextTrack, i => Next()));
                    menu.AddItem(new MenuItem(i => Localization.Current.PrevTrack, i => Prev()));
                    menu.AddItem(new MenuItem(i => Localization.Current.Settings, MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = settingsScreen });
                    menu.AddBackButton();
                    menu.Updated += (m, a) =>
                    {
                        if (a.Reason == MenuScreenUpdateReason.StatusChanged)
                        {
                            settingsScreen.Status = m.Status;
                            nowPlayingScreen.Refresh();
                        }
                    };
                }
                return menu;
            }
        }

        void UpdateNowPlayingScreen(MenuScreen nowPlayingScreen, TrackInfo nowPlaying)
        {
            nowPlayingScreen.IsUpdateSuspended = true;

            nowPlayingScreen.Status = nowPlaying.GetTrackPlaylistPosition();

            nowPlayingScreen.ClearItems();
            if (NowPlayingTagsSeparatedRows)
            {
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Title))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.Title));
                }
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Artist))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => CharIcons.BordmonitorBull + " " + Localization.Current.Artist + ":"));
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.Artist));
                }
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Album))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => CharIcons.BordmonitorBull + " " + Localization.Current.Album + ":"));
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.Album));
                }
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Genre))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => CharIcons.BordmonitorBull + " " + Localization.Current.Genre + ":"));
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.Genre));
                }
            }
            else
            {
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Title))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.GetTitleWithLabel()));
                }
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Artist))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.GetArtistWithLabel()));
                }
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Album))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.GetAlbumWithLabel()));
                }
                if (!StringHelpers.IsNullOrEmpty(nowPlaying.Genre))
                {
                    nowPlayingScreen.AddItem(new MenuItem(i => nowPlaying.GetGenreWithLabel()));
                }
            }
            nowPlayingScreen.AddBackButton();

            nowPlayingScreen.IsUpdateSuspended = false;
            nowPlayingScreen.Refresh();
        }

        public override bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
            protected set
            {
                if (isPlaying == value)
                {
                    return;
                }
                isPlaying = value;
                OnIsPlayingChanged(value);
                Logger.Info(value ? "Playing" : "Paused", "BT");
            }
        }

        protected override void OnPlayerHostStateChanged(PlayerHostState playerHostState)
        {
            base.OnPlayerHostStateChanged(playerHostState);

            switch (playerHostState)
            {
                case PlayerHostState.On:
                    Connect();
                    break;
                case PlayerHostState.StandBy:
                    // disconn av
                    break;
                case PlayerHostState.Off:
                    Disconnect();
                    break;
            }
        }

        protected override void SetPlaying(bool value)
        {
            if (!IsConnected)
            {
                return;
            }
            if (IsCurrentPlayer && value)
            {
                // TODO check current status
                SendCommand("AVRCP PLAY");
            }
            else
            {
                SendCommand("AVRCP PAUSE");
            }
        }


    }
}

#endif
