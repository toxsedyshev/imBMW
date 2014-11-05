using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using System.Text;
using imBMW.Tools;
using imBMW.Features.Localizations;
using imBMW.Features.Menu;
using imBMW.Multimedia.Models;

namespace imBMW.Multimedia
{
    public class BluetoothWT32 : AudioPlayerBase
    {
        SerialPortBase port;

        public BluetoothWT32(string port)
        {
            Name = "Bluetooth";

            queue = new QueueThreadWorker(ProcessSendCommand);

            this.port = new SerialInterruptPort(new SerialPortConfiguration(port, BaudRate.Baudrate115200, Parity.None, 8, StopBits.One, true), Cpu.Pin.GPIO_NONE, 0, 16, 10);
            this.port.NewLine = "\n";
            this.port.DataReceived += port_DataReceived;
        }

        QueueThreadWorker queue;
        MenuScreen menu;
        byte[] btBuffer;
        string lastCommand = "";
        string connectedAddress;
        string lastConnectedAddress;
        bool isHFPConnected;
        int initStep = 0;
        bool isSleeping;

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

        void SendCommand(string command, string param = null)
        {
            if (isSleeping)
            {
                queue.Enqueue(" AT"); // wakes up after SLEEP 
                isSleeping = false;
            }
            lastCommand = command;
            if (param != null)
            {
                command += " " + param;
            }
            queue.Enqueue(command);
        }

        void ProcessSendCommand(object o)
        {
            var s = (string)o;
            port.WriteLine(s);
            Logger.Info(s, "> BT");
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.AvailableBytes == 0)
            {
                return;
            }
            var data = port.ReadAvailable();
            if (btBuffer == null)
            {
                btBuffer = data;
            }
            else
            {
                btBuffer = btBuffer.Combine(data);
            }
            int index;
            while (btBuffer != null && ((index = btBuffer.IndexOf(0x0D)) != -1 || (index = btBuffer.IndexOf(0x0A)) != -1))
            {
                var s = index == 0 ? String.Empty : Encoding.UTF8.GetString(btBuffer.SkipAndTake(0, index));
                if (s != String.Empty)
                {
                    ProcessBTNotification(s);
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
        
        private void ProcessBTNotification(string s)
        {
            s = s.Trim();
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
                            SendCommand("SET PROFILE A2DP", "SINK");
                            SendCommand("SET PROFILE AVRCP", "CONTROLLER");
                            SendCommand("SET BT CLASS", "240408");
                            SendCommand("SET BT SSP 3 0");
                            SendCommand("SET BT AUTH * 0000");
                            SendCommand("SET BT NAME imBMW");
                            SendCommand("RESET");
                            break;
                        default:
                            // inited
                            SendCommand("VOLUME 15");
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
                                //SendCommand("AVRCP PDU 50 0");
                                SendCommand("AVRCP PDU 20 0"); // get now playing
                                //SendCommand("AVRCP PDU 10 3");
                                SendCommand("AVRCP PDU 31 1");
                                SendCommand("AVRCP PDU 31 2");
                                SendCommand("AVRCP PDU 31 9");
                                Play();
                                //SendCommand("AVRCP PDU 31 a");
                                //SendCommand("AVRCP PDU 31 b");
                                //SendCommand("AVRCP UP");
                                break;
                        }
                    }
                    break;
                case "AVRCP":
                    if (plen > 5 && p[1] == "GET_ELEMENT_ATTRIBUTES_RSP")
                    {
                        var track = p[5]; // TODO find closing quote
                    }
                    else if (plen > 3 && p[1] == "REGISTER_NOTIFICATION_RSP")
                    {
                        switch (p[3])
                        {
                            case "PLAYBACK_STATUS_CHANGED":
                                IsPlaying = p[4] == "PLAYING";
                                if (p[2] == "CHANGED")
                                {
                                    SendCommand("AVRCP PDU 31 1");
                                }
                                break;
                            case "TRACK_CHANGED":
                                // [4] and [5] = 0 and 0 ?
                                if (p[2] == "CHANGED")
                                {
                                    SendCommand("AVRCP PDU 20 0");
                                    SendCommand("AVRCP PDU 31 2");
                                }
                                break;
                            case "NOW_PLAYING_CHANGED":
                                if (p[2] == "CHANGED")
                                {
                                    SendCommand("AVRCP PDU 31 9");
                                }
                                break;
                        }
                    }
                    break;
                case "NO":
                    if (plen > 2 && p[1] == "CARRIER" && p[2] == "0")
                    {
                        IsConnected = false;
                    }
                    break;
            }
        }

        void OnA2DPConnected(string address, string link)
        {
            ConnectedAddress = address;
            if (PlayerHostState == Multimedia.PlayerHostState.Off)
            {
                Disconnect();
                return;
            }
            if (link == "1")
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
                SendCommand("DELAY 0 1000 SLEEP");
                isSleeping = true;
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
                SendCommand("AVRCP DOWN");
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
                    settingsScreen.AddItem(new MenuItem(i => Localization.Current.Volume + " +", i => VolumeUp()));
                    settingsScreen.AddItem(new MenuItem(i => Localization.Current.Volume + " -", i => VolumeDown()));
                    settingsScreen.AddItem(new MenuItem(i => IsConnected ? Localization.Current.Disconnect : Localization.Current.Connect, i => { if (IsConnected) Disconnect(); else Connect(); }), 3);
                    settingsScreen.AddBackButton();

                    var nowPlayingScreen = new MenuScreen(s => menu.Status);
                    nowPlayingScreen.AddItem(new MenuItem(i => NowPlaying.GetTitleWithLabel()));
                    nowPlayingScreen.AddItem(new MenuItem(i => NowPlaying.GetArtistWithLabel()));
                    nowPlayingScreen.AddItem(new MenuItem(i => NowPlaying.GetAlbumWithLabel()));
                    nowPlayingScreen.AddItem(new MenuItem(i => NowPlaying.GetGenreWithLabel()));
                    nowPlayingScreen.AddBackButton();

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

                    NowPlayingChanged += (p, nowPlaying) =>
                    {
                        nowPlayingScreen.WithUpdateSuspended(m => m.Status = nowPlaying.GetTrackPlaylistPosition());
                        nowPlayingScreen.Refresh();
                    };
                }
                return menu;
            }
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
