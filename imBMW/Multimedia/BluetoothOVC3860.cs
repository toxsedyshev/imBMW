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
            Phones = String.Empty;
        }

        public void AddPhone(string phone)
        {
            var phoneClear = "";
            foreach (var c in phone)
            {
                if (c.IsNumeric() || c == '+')
                {
                    phoneClear += c;
                }
            }
            Phones += phoneClear + "\n";
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

                var handle = new FileStream(contactsPath, FileMode.Open, FileAccess.Read);
                var data = new byte[1000];
                int read = 0;
                uint found = 0;
                bool skip = true;
                bool parse = false;
                PhoneContact contact = new PhoneContact();
                for (int i = 0; i < handle.Length; i += read)
                {
                    handle.Seek(i, SeekOrigin.Begin);
                    read = handle.Read(data, 0, data.Length);
                    int nextLine = 0;
                    int len = 0;
                    for (int j = 0; j < read; j++)
                    {
                        var b = data[j];
                        if (b == '\n' || b == '\r')
                        {
                            if (len > 0)
                            {
                                var s = Encoding.UTF8.GetString(data, nextLine, len);
                                if (s == "BEGIN:VCARD")
                                {
                                    skip = true;
                                    parse = found >= offset;
                                }
                                else if (s == "END:VCARD")
                                {
                                    if (parse)
                                    {
                                        if (!skip)
                                        {
                                            //Logger.Info(contact.Name + " " + contact.Phones, "PH");
                                            contacts.Add(contact);
                                            if (contacts.Count == count)
                                            {
                                                return contacts;
                                            }
                                            contact = new PhoneContact();
                                        }
                                        else
                                        {
                                            contact.Name = String.Empty;
                                            contact.Phones = String.Empty;
                                        }
                                    }
                                    if (!skip)
                                    {
                                        found++;
                                    }
                                }
                                else if (s.Substring(0, 2) == "FN")
                                {
                                    if (parse)
                                    {
                                        contact.Name = s.Substring(s.LastIndexOf(':') + 1);
                                    }
                                }
                                else if (s.Substring(0, 3) == "TEL")
                                {
                                    if (parse)
                                    {
                                        contact.AddPhone(s.Substring(s.LastIndexOf(':') + 1));
                                    }
                                    skip = false;
                                }
                            }
                            nextLine = j + 1;
                            len = 0;
                        }
                        else
                        {
                            len++;
                        }
                    }
                    read = nextLine;
                    Debug.GC(true); // Logger.Info("Free memory = " + Debug.GC(true), "MEM");
                }
                handle.Close();
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
            var menu = new MenuScreen();
            menu.Title = Name;
            // TODO update status

            menu.AddItem(new MenuItem("Голосовой набор", i => SendCommand(CmdVoiceCall)));
            menu.AddItem(new MenuItem("Контакты", MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = CreateContactsScreen() });
            menu.AddBackButton();

            return menu;
        }

        protected MenuScreen CreateContactsScreen()
        {
            var menu = new MenuScreen();
            menu.Title = "Контакты";

            return menu;

            menu.AddItem(new MenuItem("< Предыдущие", MenuItemType.Button), 0); // TODO navigate
            menu.AddItem(new MenuItem("Следующие >", MenuItemType.Button), 5); // TODO test, fix and make 1

            // TODO get contacts only on screen shown
            menu.UpdateSuspended = true;
            var contacts = GetContacts(13, 7);
            var i = 2;
            foreach (var c in contacts)
            {
                var contact = c as PhoneContact;
                menu.AddItem(new MenuItem(contact.Name, MenuItemType.Button), i++); // TODO show phones
            }
            menu.UpdateSuspended = false;

            menu.AddBackButton(9);

            return menu;

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
            OnStatusChanged("AnswerCall", PlayerEvent.Voice);
        }

        public override void MFLDialLong()
        {
            SendCommand(CmdVoiceCall);
            OnStatusChanged("VoiceCall", PlayerEvent.Voice);
        }

        public override bool RandomToggle()
        {
            SendCommand(CmdEnterPairing);
            OnStatusChanged("Pairing", PlayerEvent.Wireless);
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
                    menu = new MenuScreen();
                    menu.Title = Name;
                    // TODO update status

                    var item = new MenuItem((i) => IsPlaying ? "Пауза" : "Играть", i => PlayPauseToggle());
                    IsPlayingChanged += (p, isPlaying) => item.Refresh();
                    menu.AddItem(item);
                    
                    menu.AddItem(new MenuItem("Следующий трек", i => Next()));
                    menu.AddItem(new MenuItem("Предыдущий трек", i => Prev()));
                    menu.AddBackButton();
                }
                return menu;
            }
        }

        protected override void OnIsPlayerHostActiveChanged(bool isPlayerHostActive)
        {
            base.OnIsPlayerHostActiveChanged(isPlayerHostActive);

            if (isPlayerHostActive)
            {
                SendCommand("CC"); // conn hfp
                SendCommand("MI"); // conn av
                //SendCommand("MG"); // enable auto conn
            }
            else
            {
                //SendCommand("MH"); // disable auto conn
                //SendCommand("CD"); // disconn hfp
                //SendCommand("MJ"); // disconn av
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

        void SendCommand(string command, string param = null)
        {
            command = "AT#" + command;
            if (param != null)
            {
                command += param;
            }
            queue.Enqueue(command);
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
            var s = port.Encoding.GetString(data);
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

        void ProcessBTNotification(string s)
        {
            switch (s)
            {
                /*case "MG3":
                    SendCommand("PB");
                    Thread.Sleep(500);
                    SendCommand("PC");
                    break;*/
                case "MR":
                    if (!IsEnabled)
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
                case "MX":
                    Logger.Info("Next", "BT");
                    break;
                case "MS":
                    Logger.Info("Prev", "BT");
                    break;
                case "IV":
                    Logger.Info("Connected", "BT");
                    OnStatusChanged("Connected", PlayerEvent.Wireless);
                    //SendCommand("MI"); // conn av
                    if (IsEnabled)
                    {
                        Play();
                    }
                    break;
                case "II":
                    IsPlaying = false;
                    Logger.Info("Waiting", "BT");
                    OnStatusChanged("Waiting", PlayerEvent.Wireless);
                    break;
                case "IA":
                    IsPlaying = false;
                    Logger.Info("Disconnected", "BT");
                    OnStatusChanged("Disconnect", PlayerEvent.Wireless);
                    break;
                case "IJ2":
                    IsPlaying = false;
                    Logger.Info("Cancel pairing", "BT");
                    OnStatusChanged("No pair", PlayerEvent.Wireless);
                    break;
                default:
                    if (s.IsNumeric())
                    {
                        Logger.Info("Phone call: " + s, "BT");
                        OnStatusChanged(s, PlayerEvent.Call);
                    }
                    else
                    {
                        Logger.Info(s, "BT<");
                    }
                    break;
            }
        }

        #endregion
    }
}
