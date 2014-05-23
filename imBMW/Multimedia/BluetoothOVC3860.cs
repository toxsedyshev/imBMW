using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections;
using imBMW.Features.Menu;
using imBMW.Features.Menu.Screens;
using imBMW.Features.Localizations;

namespace imBMW.Multimedia
{
    #region Enums, handlers, etc

    public class PhoneContact
    {
        public string Name { get; set; }

        /*public ArrayList Phones { get; set; }

        public void AddPhone(string phone)
        {
            if (Phones == null)
            {
                Phones = new ArrayList();
            }
            var phoneClear = "";
            foreach (var c in phone)
            {
                if (c.IsNumeric() || c == '+')
                {
                    phoneClear += c;
                }
            }
            Phones.Add(phoneClear);
        }*/

        public string Phones { get; set; }

        public PhoneContact()
        {
            Name = String.Empty;
            Phones = String.Empty;
        }

        public void AddPhone(string phone)
        {
            if (Phones != String.Empty)
            {
                return; // TODO more than one phone
            }
            var phoneClear = "";
            foreach (var c in phone)
            {
                if (c.IsNumeric() || c == '+')
                {
                    phoneClear += c;
                }
            }
            Phones += phoneClear;// +"\n";
        }
    }

    #endregion

    /// <summary>
    /// Bluetooth module Bolutek BLK-MD-SPK-B based on OmniVision OVC3860 that supports A2DP and AVRCP profiles
    /// Communicates via COM port
    /// </summary>
    public class BluetoothOVC3860 : AudioPlayerBase
    {
        SerialPortBase port;
        QueueThreadWorker queue;
        MenuScreen menu;
        string contactsPath;

        /// <summary>
        /// </summary>
        /// <param name="port">COM port name</param>
        /// <param name="contactsPath">Path to contacts VCF file</param>
        public BluetoothOVC3860(string port, string contactsPath = null)
        {
            Name = "Bluetooth";

            queue = new QueueThreadWorker(ProcessSendCommand);

            this.port = new SerialInterruptPort(new SerialPortConfiguration(port, BaudRate.Baudrate115200), Cpu.Pin.GPIO_NONE, 0, 16, 10);
            this.port.DataReceived += port_DataReceived;

            if (contactsPath != null)
            {
                if (File.Exists(contactsPath))
                {
                    this.contactsPath = contactsPath;
                }
                else
                {
                    Logger.Info("No contacts file " + contactsPath);
                }
            }

            HomeScreen.Instance.PhoneScreen = CreatePhoneScreen();

            //SendCommand("MH"); // disable auto conn
            VolumeUp(); // TODO make loop: volume up
        }

        #region Public methods

        public ArrayList GetContacts(uint offset, uint count)
        {
            // TODO regenerate file
            var contacts = new ArrayList();
            try
            {
                if (contactsPath == null || !File.Exists(contactsPath))
                {
                    Logger.Info("No contacts file");
                    return contacts;
                }

                using (var sr = new StreamReader(contactsPath))
                {
                    uint found = 0;
                    bool hasPhone = false;
                    PhoneContact contact = null;

                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s == string.Empty)
                        {
                            continue;
                        }
                        switch (s)
                        {
                            case "BEGIN:VCARD":
                                Debug.GC(true); // Logger.Info("Free memory = " + Debug.GC(true), "MEM");
                                hasPhone = false;
                                if (found >= offset)
                                {
                                    contact = new PhoneContact();
                                }
                                break;
                            case "END:VCARD":
                                if (hasPhone)
                                {
                                    if (contact != null)
                                    {
                                        contacts.Add(contact);
                                        if (contacts.Count == count)
                                        {
                                            return contacts;
                                        }
                                    }
                                    found++;
                                }
                                break;
                            default:
                                if (s.Substring(0, 2) == "FN")
                                {
                                    if (contact != null)
                                    {
                                        contact.Name = s.Split(':')[1];
                                    }
                                }
                                else if (s.Substring(0, 3) == "TEL")
                                {
                                    if (contact != null)
                                    {
                                        contact.AddPhone(s.Split(':')[1]);
                                    }
                                    hasPhone = true;
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "contacts loading");
            }
            return contacts;
        }

        #endregion

        #region Protected methods

        protected MenuScreen CreatePhoneScreen()
        {
            var screen = new MenuScreen(Name);
            screen.AddItem(new MenuItem(i => Localization.Current.VoiceCall, i => SendCommand(CmdVoiceCall)));
            screen.AddItem(new MenuItem(i => Localization.Current.Contacts, MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = CreateContactsScreen() });
            screen.AddBackButton();
            return screen;
        }

        int contactsPerPage = 7;
        int offset = 0;
        MenuScreen contactsScreen;

        protected MenuScreen CreateContactsScreen()
        {
            contactsPerPage = MenuScreen.MaxItemsCount - 3;

            contactsScreen = new MenuScreen(s => Localization.Current.Contacts);
            contactsScreen.AddItem(new MenuItem(i => "< " + Localization.Current.PrevItems, i => { offset -= contactsPerPage; SetContactsScreenItems(); }), 0); // TODO navigate
            contactsScreen.AddItem(new MenuItem(i => Localization.Current.NextItems + " >", i => { offset += contactsPerPage; SetContactsScreenItems(); }), 1); // TODO test, fix and make 1
            contactsScreen.AddBackButton(MenuScreen.MaxItemsCount - 1);

            contactsScreen.NavigatedTo += s =>
            {
                offset = 0; // TODO don't scroll on navigate back
                SetContactsScreenItems();
            };

            return contactsScreen;
        }

        protected void SetContactsScreenItems()
        {
            if (offset < 0)
            {
                offset = 0;
            }
            contactsScreen.Status = Localization.Current.Refreshing;
            var contacts = GetContacts((uint)offset, (uint)contactsPerPage);
            if (contacts.Count == 0 && offset > 0)
            {
                offset = 0;
                SetContactsScreenItems();
                return;
            }

            contactsScreen.IsUpdateSuspended = true;
            var i = 2;
            if (contacts.Count == 0)
            {
                contactsScreen.AddItem(new MenuItem(Localization.Current.NoContacts), i++);
            }
            else
            {
                foreach (var c in contacts)
                {
                    var contact = (PhoneContact)c;
                    contactsScreen.AddItem(new MenuItem(contact.Name, it => CallPhone(contact.Phones)), i++); // TODO show phones
                }
            }
            contactsScreen.IsUpdateSuspended = false;
            contactsScreen.Status = "";
        }

        protected override void SetPlaying(bool value)
        {
            if (IsCurrentPlayer)
            {
                if (value != IsPlaying)
                {
                    SendCommand(CmdPlayPause);
                }
            }
            else
            {
                SendCommand(CmdStop);
            }
        }

        #endregion

        #region IAudioPlayer members

        public override void Next()
        {
            SetPlaying(true);
            SendCommand(CmdNext);
            OnStatusChanged(PlayerEvent.Next);
        }

        public override void Prev()
        {
            SetPlaying(true);
            SendCommand(CmdPrev);
            OnStatusChanged(PlayerEvent.Prev);
        }

        public override void MFLRT()
        {
            PlayPauseToggle();
        }

        public override void MFLDial()
        {
            SendCommand(CmdAnswer);
            OnStatusChanged("AnswerCall", PlayerEvent.Voice); // TODO call status and reject
        }

        public override void MFLDialLong()
        {
            SendCommand(CmdVoiceCall);
            OnStatusChanged("VoiceCall", PlayerEvent.Voice);
        }

        public override bool RandomToggle()
        {
            // TODO
            //SendCommand(CmdEnterPairing);
            //OnStatusChanged("Pairing", PlayerEvent.Wireless);
            return false;
        }

        public override void VolumeUp()
        {
            SendCommand(CmdVolumeUp);
        }

        public override void VolumeDown()
        {
            SendCommand(CmdVolumeDown);
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

        public override MenuScreen Menu
        {
            get
            {
                if (menu == null)
                {
                    menu = new MenuScreen(Name);

                    var playerSettings = new MenuScreen(s => Localization.Current.Settings);
                    playerSettings.Status = Name;
                    playerSettings.AddItem(new MenuItem(i => Localization.Current.Volume + " +", i => VolumeUp()));
                    playerSettings.AddItem(new MenuItem(i => Localization.Current.Volume + " -", i => VolumeDown()));
                    playerSettings.AddItem(new MenuItem(i => Localization.Current.Reconnect, i => Reconnect()), 3);
                    playerSettings.AddItem(new MenuItem(i => Localization.Current.Pair, i =>
                    {
                        if (lastCommand == "CA")
                        {
                            SendCommand("CB"); // cancel pair
                        }
                        else
                        {
                            SendCommand("CA"); // pair
                        }
                    }));
                    playerSettings.AddBackButton();

                    menu.AddItem(new MenuItem(i => IsPlaying ? Localization.Current.Pause : Localization.Current.Play, i => PlayPauseToggle()));
                    menu.AddItem(new MenuItem(i => Localization.Current.NextTrack, i => Next()));
                    menu.AddItem(new MenuItem(i => Localization.Current.PrevTrack, i => Prev()));
                    menu.AddItem(new MenuItem(i => Localization.Current.Settings, MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = playerSettings });
                    menu.AddBackButton();
                    menu.Updated += (m, a) => { if (a.Reason == MenuScreenUpdateReason.StatusChanged) { playerSettings.Status = m.Status; } };
                }
                return menu;
            }
        }

        protected override void OnPlayerHostStateChanged(PlayerHostState playerHostState)
        {
            base.OnPlayerHostStateChanged(playerHostState);

            switch (playerHostState)
            {
                case PlayerHostState.On:
                    if (!connected)
                    {
                        Reconnect();
                    }
                    break;
                case PlayerHostState.StandBy:
                    //SendCommand("MJ"); // disconn av
                    break;
                case PlayerHostState.Off:
                    //SendCommand("VX");   // power off ool
                    //SendCommand("MH"); // disable auto conn
                    //SendCommand("CD"); // disconn hfp
                    break;
            }
        }

        #endregion

        #region OVC3860 members

        const string CmdPlayPause = "MA";
        const string CmdStop = "MC";
        const string CmdNext = "MD";
        const string CmdPrev = "ME";
        const string CmdVolumeUp = "VU";
        const string CmdVolumeDown = "VD";
        const string CmdVoiceCall = "CI";
        const string CmdEnterPairing = "CA";
        const string CmdAnswer = "CE";
        const string CmdCall = "CW";

        string lastCommand;
        bool connected;

        public void Reconnect()
        {
            if (connected)
            {
                //SendCommand(CmdStop);
                //SendCommand("CD");   // disconn hfp
                SendCommand("MJ");   // disconn av
                //SendCommand("CZ");   // reset
            }
            else
            {
                //SendCommand("VI"); // start inquiry
                //SendCommand("MI"); // conn av
                SendCommand("CC"); // conn hfp
                //SendCommand("MG"); // enable auto conn
            }
        }

        public void CallPhone(string number)
        {
            SendCommand(CmdCall, number);
        }

        void SendCommand(string command, string param = null)
        {
            if (param != null)
            {
                command += param;
            }
            lastCommand = command;
            queue.Enqueue("AT#" + command);
        }

        void ProcessSendCommand(object o)
        {
            var s = (string)o;
            port.WriteLine(s);
            Logger.Info(s, "BT>");
        }

        string btBuffer = "";

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.AvailableBytes == 0)
            {
                return;
            }
            var data = port.ReadAvailable();
            var s = ASCIIEncoding.GetString(data);
            foreach (var c in s)
            {
                if (c == '\r' || c == '\n')
                {
                    if (btBuffer != "")
                    {
                        ProcessBTNotification(btBuffer);
                    }
                    btBuffer = "";
                }
                else
                {
                    btBuffer += c;
                }
            }
        }

        const int PlayAfterConnectMilliseconds = 1000;
        Timer playAfterConnectTimer;

        void ProcessBTNotification(string s)
        {
            switch (s)
            {
                case "OK":
                    ProcessBTOK();
                    break;
                case "MG1":
                    Logger.Info("HFP waiting", "BT");
                    connected = false;
                    SendCommand("CC"); // conn hfp
                    break;
                case "MG3":
                    Logger.Info("HFP connected", "BT");
                    connected = true;
                    SendCommand("MI"); // conn av
                    break;
                case "MF01":
                    Logger.Info("Auto Power = On, Auto Answer = Off", "BT");
                    SendCommand("CY"); // query hfp status
                    break;
                case "MY":
                    Logger.Info("AV disconnected", "BT");
                    if (lastCommand == "MJ")
                    {
                        SendCommand("CD");   // disconn hfp
                    }
                    break;
                case "MR":
                    if (!IsEnabled && lastCommand != CmdStop)
                    {
                        SendCommand(CmdStop);
                    }
                    else
                    {
                        IsPlaying = true;
                    }
                    break;
                case "MP":
                    IsPlaying = false;
                    break;
                case "MA":
                    /*if (IsEnabled && lastCommand == "MI")
                    {
                        Play();
                    }
                    else
                    {
                        IsPlaying = false;
                    }*/
                    break;
                case "MB":
                    //IsPlaying = true; // sent even when paused?
                    break;
                case "MX":
                    Logger.Info("NextItems", "BT");
                    break;
                case "MS":
                    Logger.Info("Previous", "BT");
                    break;
                case "IV":
                    connected = true;
                    Logger.Info("Connected", "BT");
                    OnStatusChanged(Localization.Current.Connected, PlayerEvent.Wireless);
                    SendCommand("MI"); // conn av
                    if (IsEnabled)
                    {
                        playAfterConnectTimer = new Timer(delegate
                        {
                            Play();
                        }, null, PlayAfterConnectMilliseconds, 0);
                    }
                    break;
                case "II":
                    connected = false;
                    IsPlaying = false;
                    Logger.Info("Waiting", "BT");
                    OnStatusChanged(Localization.Current.Waiting, PlayerEvent.Wireless);
                    break;
                case "IA":
                    connected = false;
                    IsPlaying = false;
                    Logger.Info("Disconnected", "BT");
                    OnStatusChanged(Localization.Current.Disconnected, PlayerEvent.Wireless);
                    if (lastCommand == "CD")
                    {
                        Reconnect();
                    }
                    break;
                case "IJ2":
                    connected = false;
                    IsPlaying = false;
                    Logger.Info("Cancel pairing", "BT");
                    OnStatusChanged(Localization.Current.NotPaired, PlayerEvent.Wireless);
                    break;
                default:
                    if (s.IsNumeric())
                    {
                        Logger.Info("Phone call: " + s, "BT");
                        OnStatusChanged(s, PlayerEvent.IncomingCall);
                    }
                    else
                    {
                        Logger.Info(s, "BT<");
                    }
                    break;
            }
        }

        void ProcessBTOK()
        {
            switch (lastCommand)
            {
                case "CC":
                    SendCommand("CY"); // query hfp status
                    break;
                case "MJ":
                    SendCommand("CD"); // disconn hfp
                    break;
                case "MI":
                    Play();
                    break;
            }
        }

        #endregion
    }
}
