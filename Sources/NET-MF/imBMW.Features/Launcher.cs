using GHI.IO;
using GHI.IO.Storage;
using imBMW.Features;
using imBMW.Features.CanBus;
using imBMW.Features.CanBus.Adapters;
using imBMW.Features.CanBus.Devices;
using imBMW.Features.Localizations;
using imBMW.Features.Menu;
using imBMW.Features.Menu.Screens;
using imBMW.iBus;
using imBMW.iBus.Devices.Emulators;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;
using imBMW.Tools;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;
using System;
using System.IO.Ports;
using System.Threading;

namespace imBMW
{
    public class LauncherSettings
    {
        public string HWVersion { get; set; }

        public Cpu.Pin LEDPin { get; set; }

        public string iBusPort { get; set; }

        public Cpu.Pin iBusBusyPin { get; set; }

        public string MediaShieldPort { get; set; }

        public Cpu.Pin MediaShieldLED { get; set; }

        public SDCard.SDInterface SDInterface { get; set; }
        
        public ControllerAreaNetwork.Channel CanBus { get; set; }

        public bool E65Seats { get; set; }
    }

    public delegate void SettingsOverrideDelegate(Settings settings);

    public class Launcher
    {
        string version = "FW1.1.13";

        LauncherSettings launcherSettings;
        SettingsOverrideDelegate settingsOverride;

        OutputPort LED;
        OutputPort ShieldLED;

        IAudioPlayer player;
        
        public Launcher(LauncherSettings launcherSettings, SettingsOverrideDelegate settingsOverride = null)
        {
            if (launcherSettings == null)
            {
                throw new NullReferenceException("launcherSettings");
            }

            this.launcherSettings = launcherSettings;
            this.settingsOverride = settingsOverride;

            version += " " + launcherSettings.HWVersion;
        }

        void Init()
        {
            LED = new OutputPort(launcherSettings.LEDPin, false);

            #region Settings

            var sd = GetRootDirectory();

            var settings = Settings.Init(sd != null ? sd + @"\imBMW.ini" : null);

            if (settingsOverride != null)
            {
                settingsOverride(settings);
            }

            var log = settings.Log || settings.LogToSD;
#if DEBUG
            log = true;
            settings.LogMessageToASCII = true;
#else
            // already inited in debug mode
            if (settings.Log)
            {
                Logger.Logged += Logger_Logged;
                Logger.Info("Logger inited");
            }
#endif
            if (settings.LogToSD && sd != null)
            {
                FileLogger.Init(sd + @"\Logs", () =>
                {
                    VolumeInfo.GetVolumes()[0].FlushAll();
                });
            }

            Logger.Info(version);
            
            Localization.SetCurrent(settings.Language);
            Comfort.AutoLockDoors = settings.AutoLockDoors;
            Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
            Comfort.AutoCloseWindows = settings.AutoCloseWindows;
            Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;
            Logger.Info("Preferences inited");

            SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;

            #endregion

            #region iBus Manager

            // Create serial port to work with Melexis TH3122
            //ISerialPort iBusPort = new SerialPortEcho();
            ISerialPort iBusPort = new SerialPortTH3122(launcherSettings.iBusPort, launcherSettings.iBusBusyPin);
            Logger.Info("TH3122 serial port inited");

            /*InputPort jumper = new InputPort((Cpu.Pin)FEZ_Pin.Digital.An7, false, Port.ResistorMode.PullUp);
            if (!jumper.Read())
            {
                Logger.Info("Jumper installed. Starting virtual COM port");

                // Init hub between iBus port and virtual USB COM port
                ISerialPort cdc = new SerialPortCDC(USBClientController.StandardDevices.StartCDC_WithDebugging(), 0, iBus.Message.PacketLengthMax);
                iBusPort = new SerialPortHub(iBusPort, cdc);
                Logger.Info("Serial port hub started");
            }*/

            Manager.Init(iBusPort);
            Logger.Info("iBus manager inited");

            #endregion

            #region iBus IO logging

            Message sent1 = null, sent2 = null; // light "buffer" for last 2 messages
            bool isSent1 = false;
            Manager.BeforeMessageReceived += (e) =>
            {
                LED.Write(Busy(true, 1));
            };
            Manager.AfterMessageReceived += (e) =>
            {
                LED.Write(Busy(false, 1));

                if (!log)
                {
                    return;
                }
                
                var logIco = "< ";
                if (e.Message.ReceiverDescription == null)
                {
                    if (sent1 != null && sent1.Data.Compare(e.Message.Data))
                    {
                        e.Message.ReceiverDescription = sent1.ReceiverDescription;
                        logIco = "<E";
                    }
                    else if (sent2 != null && sent2.Data.Compare(e.Message.Data))
                    {
                        e.Message.ReceiverDescription = sent2.ReceiverDescription;
                        logIco = "<E";
                    }
                }
                if (settings.LogMessageToASCII && e.Message.ReceiverDescription == null)
                {
                    Logger.Info(e.Message.ToPrettyString(true, true), logIco);
                }
                else
                {
                    Logger.Info(e.Message, logIco);
                }
            };
            Manager.BeforeMessageSent += (e) =>
            {
                LED.Write(Busy(true, 2));
            };
            Manager.AfterMessageSent += (e) =>
            {
                LED.Write(Busy(false, 2));

                if (!log)
                {
                    return;
                }

                Logger.Info(e.Message, " >");
                if (isSent1)
                {
                    sent1 = e.Message;
                }
                else
                {
                    sent2 = e.Message;
                }
                isSent1 = !isSent1;
            };
            Logger.Info("iBus manager events subscribed");

            #endregion

            #region Features

#if DEBUG
            InstrumentClusterElectronics.RequestDateTimeAndSetLocal(false);
#endif

            #endregion

            #region CAN BUS

            if (launcherSettings.CanBus != 0)
            {
                var speed = CanAdapterSettings.CanSpeed.Kbps100;
                CanAdapter.Current = new CanNativeAdapter(launcherSettings.CanBus, speed);
                //var canInterrupt = Cpu.Pin.GPIO_NONE;
                //var canInterrupt = Pin.Di2;
                //CanAdapter.Current = new CanMCP2515Adapter(Pin.SPI, Pin.SPI_ChipSelect, canInterrupt, speed, CanMCP2515AdapterSettings.AdapterFrequency.Mhz8);
                if (log)
                {
                    CanAdapter.Current.MessageReceived += Can_MessageReceived;
                    CanAdapter.Current.MessageSent += Can_MessageSent;
                    CanAdapter.Current.Error += Can_ErrorReceived;
                }
                CanAdapter.Current.IsEnabled = true;

                if (launcherSettings.E65Seats)
                {
                    //HardwareButton.Toggle(Pin.Di2, pressed => { if (pressed) E65Seats.EmulatorPaused = true; else E65Seats.EmulatorPaused = false; });
                    E65Seats.Init();
                    E65Seats.AutoHeater = true;
                    E65Seats.AutoVentilation = true;
                    HomeScreen.Instance.SeatsScreen = E65SeatsScreen.Instance;
                }

                Logger.Info("CAN BUS inited");
            }

            #endregion

            #region Set iPod or Bluetooth as AUX or CDC-emulator for Bordmonitor or Radio
            
            BluetoothWT32 wt32;
            if (settings.MediaShield == "WT32")
            {
                wt32 = new BluetoothWT32(launcherSettings.MediaShieldPort, Settings.Instance.BluetoothPin);
                player = wt32;
                InternalCommunications.Register(wt32);

                //byte gain = 0;
                //HardwareButton.OnPress(Pin.Di14, () => wt32.SetMicGain((byte)(++gain % 16)));
            }
            else
            {
                player = new BluetoothOVC3860(launcherSettings.MediaShieldPort, sd != null ? sd + @"\contacts.vcf" : null);
            }
            
            MediaEmulator emulator;
            if (settings.MenuMode == MenuMode.BordmonitorAUX
                || settings.MenuMode == MenuMode.BordmonitorCDC
                || Manager.FindDevice(DeviceAddress.OnBoardMonitor))
            {
                if (player is BluetoothWT32)
                {
                    ((BluetoothWT32)player).NowPlayingTagsSeparatedRows = true;
                }
                if (settings.MenuMode == MenuMode.BordmonitorCDC)
                {
                    emulator = new CDChanger(player, true);
                    if (settings.NaviVersion == NaviVersion.MK2)
                    {
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                    }
                }
                else
                {
                    emulator = new BordmonitorAUX(player);
                }
                Bordmonitor.NaviVersion = settings.NaviVersion;
                BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                //MenuScreen.MaxItemsCount = 6;
                BordmonitorMenu.Init(emulator);

                Logger.Info("Bordmonitor menu inited");
            }
            else
            {
                Localization.Current = new RadioLocalization();
                SettingsScreen.Instance.CanChangeLanguage = false;
                MultiFunctionSteeringWheel.EmulatePhone = true;
                if (settings.MenuMode == MenuMode.MIDAUX)
                {
                    Radio.HasMID = true;
                    emulator = new MIDAUX(player);
                }
                else
                {
                    Radio.HasMID = Manager.FindDevice(DeviceAddress.MultiInfoDisplay);
                    emulator = new CDChanger(player);
                }
                var menu = RadioMenu.Init(emulator);
                menu.IsDuplicateOnIKEEnabled = true;
                menu.TelephoneModeForNavigation = settings.MenuMFLControl;
                Logger.Info("Radio menu inited" + (Radio.HasMID ? " with MID" : ""));
            }

            player.IsShowStatusOnIKEEnabled = true;

            ShieldLED = new OutputPort(launcherSettings.MediaShieldLED, false);
            player.IsPlayingChanged += (p, e) =>
            {
                ShieldLED.Write(e.IsPlaying);
                RefreshLEDs();
            };
            player.StatusChanged += (p, e) =>
            {
                if (e.Event == PlayerEvent.IncomingCall && !p.IsEnabled)
                {
                    InstrumentClusterElectronics.Gong1();
                }
            };
            Logger.Info("Player events subscribed");

            #endregion
            
            RefreshLEDs();
            
            LED.Write(true);
            Thread.Sleep(50);
            LED.Write(false);
            Logger.Info("LED blinked - inited");
        }

        private static void Can_MessageSent(CanAdapter can, CanMessage message)
        {
            Logger.Info(message.ToString(), "CAN>");
        }

        private static void Can_ErrorReceived(CanAdapter can, string message)
        {
            Logger.Error(message, "CAN-ERR");
        }

        private static void Can_MessageReceived(CanAdapter can, CanMessage message)
        {
            Logger.Info(message.ToString(), "CAN");
        }
        
        void RefreshLEDs()
        {
            if (!Manager.Inited)
            {
                return;
            }
            byte b = 0;
            if (error)
            {
                b = b.AddBit(0);
            }
            if (player != null && player.IsPlaying)
            {
                b = b.AddBit(4);
            }
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, b));
        }

        SDCard sdCard;

        public string GetRootDirectory()
        {
            try
            {
                Logger.Info("Mount", "SD");
                sdCard = new SDCard(launcherSettings.SDInterface);
                sdCard.Mount();
                Logger.Info("Mounted", "SD");
            }
            catch
            {
                Logger.Info("Not mounted", "SD");
                return null;
            }
            try
            {
                if (VolumeInfo.GetVolumes()[0].IsFormatted)
                {
                    string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                    Logger.Info("Root directory: " + rootDirectory, "SD");
                    /*string[] files = Directory.GetFiles(rootDirectory);
                    string[] folders = Directory.GetDirectories(rootDirectory);

                    Logger.Info("Files available on " + rootDirectory + ":");
                    for (int i = 0; i < files.Length; i++)
                        Logger.Info(files[i]);

                    Logger.Info("Folders available on " + rootDirectory + ":");
                    for (int i = 0; i < folders.Length; i++)
                        Logger.Info(folders[i]);*/
                    return rootDirectory;
                }
                else
                {
                    Logger.Error("Card not formatted!", "SD");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "getting root directory", "SD");
            }
            return null;
        }
        
        byte busy = 0;
        bool error = false;

        bool Busy(bool busy, byte type)
        {
            if (busy)
            {
                this.busy = this.busy.AddBit(type);
            }
            else
            {
                this.busy = this.busy.RemoveBit(type);
            }
            return this.busy > 0;
        }

        public void Run()
        {

            Debug.Print("Starting..");

#if DEBUG
            Logger.Logged += Logger_Logged;
            Logger.Info("Logger inited");
#endif

            try
            {
                Init();

                Debug.EnableGCMessages(false);
                Logger.Info("Started!");
                
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                LED.Write(true);
                Logger.Error(ex, "while modules initialization");
            }
        }

        void Logger_Logged(LoggerArgs args)
        {
            if (args.Priority == Tools.LogPriority.Error)
            {
                // store errors to arraylist
                error = true;
                //RefreshLEDs();
            }
            Debug.Print(args.LogString);
        }
    }
}
