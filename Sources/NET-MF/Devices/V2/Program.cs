using GHI.IO.Storage;
using imBMW.Devices.V2.Hardware;
using imBMW.Features;
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
using System.Collections;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace imBMW.Devices.V2
{
    public class Program
    {
        const string version = "FW1.0.11 HW2";

        static OutputPort LED;
        static OutputPort ShieldLED;

        static IAudioPlayer player;

        static Timer fakeLogsTimer;

        static void Init()
        {
            LED = new OutputPort(Pin.LED, false);

            #region Settings

            var sd = GetRootDirectory();
            
            var settings = Settings.Init(sd != null ? sd + @"\imBMW.ini" : null);
            var log = settings.Log || settings.LogToSD;

            Localization.SetCurrent(settings.Language);
            Features.Comfort.AutoLockDoors = settings.AutoLockDoors;
            Features.Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
            Features.Comfort.AutoCloseWindows = settings.AutoCloseWindows;
            Features.Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;
            Logger.Info("Preferences inited");

            #if DEBUG
            log = true;
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
            SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;

            #endregion

            #region iBus Manager

            // Create serial port to work with Melexis TH3122
            //ISerialPort iBusPort = new SerialPortEcho();
            ISerialPort iBusPort = new SerialPortTH3122(Serial.COM3, Pin.TH3122SENSTA, true);
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

            iBus.Manager.Init(iBusPort);
            Logger.Info("iBus manager inited");

            #endregion

            #region iBus IO logging

            Message sent1 = null, sent2 = null; // light "buffer" for last 2 messages
            bool isSent1 = false;
            iBus.Manager.BeforeMessageReceived += (e) =>
            {
                LED.Write(Busy(true, 1));
            };
            iBus.Manager.AfterMessageReceived += (e) =>
            {
                LED.Write(Busy(false, 1));

                if (!log)
                {
                    return;
                }

                // Show only messages which are described
                if (e.Message.Describe() == null) { return; }
                // Filter CDC emulator messages echoed by iBus
                //if (e.Message.SourceDevice == iBus.DeviceAddress.CDChanger) { return; }
                if (e.Message.SourceDevice != DeviceAddress.Radio
                    && e.Message.DestinationDevice != DeviceAddress.Radio
                    && e.Message.SourceDevice != DeviceAddress.GraphicsNavigationDriver
                    && e.Message.SourceDevice != DeviceAddress.Diagnostic && e.Message.DestinationDevice != DeviceAddress.Diagnostic)
                {
                    //return;
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
                if (settings.LogMessageToASCII)
                {
                    Logger.Info(e.Message.ToPrettyString(true, true), logIco);
                }
                else
                {
                    Logger.Info(e.Message, logIco);
                }
                /*if (e.Message.ReceiverDescription == null)
                {
                    Logger.Info(ASCIIEncoding.GetString(e.Message.Data));
                }*/
                //Logger.Info(e.Message.PacketDump);
            };
            iBus.Manager.BeforeMessageSent += (e) =>
            {
                LED.Write(Busy(true, 2));
            };
            iBus.Manager.AfterMessageSent += (e) =>
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

            #region Set iPod or Bluetooth as AUX or CDC-emulator for Bordmonitor or Radio

            //
            //Bordmonitor.ReplyToScreenUpdates = true;
            //settings.MenuMode = MenuMode.RadioCDC;
            //settings.MediaShield = "WT32";
            //settings.BluetoothPin = "1111";
            //

            BluetoothWT32 wt32;
            if (settings.MediaShield == "WT32")
            {
                wt32 = new BluetoothWT32(Serial.COM2, Settings.Instance.BluetoothPin);
                player = wt32;
                InternalCommunications.Register(wt32);

                byte gain = 0;
                Button.OnPress(Pin.Di14, () => wt32.SetMicGain((byte)(++gain % 16)));
            }
            else
            {
                player = new BluetoothOVC3860(Serial.COM2, sd != null ? sd + @"\contacts.vcf" : null);
                //player = new iPodViaHeadset(Pin.PC2);
            }

            //
            //player.IsCurrentPlayer = true;
            //player.PlayerHostState = PlayerHostState.On;
            //

            MediaEmulator emulator;
            if (settings.MenuMode != Tools.MenuMode.RadioCDC || Manager.FindDevice(DeviceAddress.OnBoardMonitor))
            {
                if (player is BluetoothWT32)
                {
                    ((BluetoothWT32)player).NowPlayingTagsSeparatedRows = true;
                }
                if (settings.MenuMode == Tools.MenuMode.BordmonitorCDC)
                {
                    emulator = new CDChanger(player);
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
                Radio.HasMID = Manager.FindDevice(DeviceAddress.MultiInfoDisplay);
                var menu = RadioMenu.Init(new CDChanger(player));
                menu.TelephoneModeForNavigation = settings.MenuMFLControl;
                Logger.Info("Radio menu inited" + (Radio.HasMID ? " with MID" : ""));
            }

            ShieldLED = new OutputPort(Pin.Di10, false);
            player.IsPlayingChanged += (p, s) =>
            {
                ShieldLED.Write(s);
                RefreshLEDs();
            };
            player.StatusChanged += (p, s, e) =>
            {
                if (e == PlayerEvent.IncomingCall && !p.IsEnabled)
                {
                    InstrumentClusterElectronics.Gong1();
                }
            };
            Logger.Info("Player events subscribed");

            #endregion

            /*blinkerTimer = new Timer((s) =>
            {
                if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off && !blinkerOn)
                {
                    return;
                }
                blinkerOn = !blinkerOn;
                RefreshLEDs();
            }, null, 0, 3000);*/

            RefreshLEDs();

            //
            //var ign = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, "Ignition ACC", 0x11, 0x01);
            //Manager.EnqueueMessage(ign);
            //Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.InstrumentClusterElectronics, m =>
            //{
            //    if (m.Data.Compare(0x10))
            //    {
            //        Manager.EnqueueMessage(ign);
            //    }
            //});
            //var b = Manager.FindDevice(DeviceAddress.Radio);
            //

            LED.Write(true);
            Thread.Sleep(50);
            LED.Write(false);
            Logger.Info("LED blinked - inited");
        }

        static bool mflPhone = false;

        static void InitTest()
        {
            /*
            Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.Broadcast, 0x11, 0x01));
            Button.OnPress(Pin.Di14, () => { Manager.EnqueueMessage(new Message(DeviceAddress.MultiFunctionSteeringWheel, DeviceAddress.Broadcast, 0x3B, (byte)((mflPhone = !mflPhone) ? 0x40 : 0x00))); });
            Button.OnPress(Pin.Di15, () => { Manager.EnqueueMessage(new Message(DeviceAddress.MultiFunctionSteeringWheel, DeviceAddress.Radio, 0x3B, 0x01)); });
            */
        }

        static void RefreshLEDs()
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
            if (blinkerOn)
            {
                //b = b.AddBit(2);
            }
            if (player != null && player.IsPlaying)
            {
                b = b.AddBit(4);
            }
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, b));
        }

        static SDCard sdCard;

        public static string GetRootDirectory()
        {
            try
            {
                Logger.Info("Mount", "SD");
                sdCard = new SDCard(SDCard.SDInterface.MCI);
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

        static Timer blinkerTimer;
        static bool blinkerOn = true;

        static byte busy = 0;
        static bool error = false;

        static bool Busy(bool busy, byte type)
        {
            if (busy)
            {
                Program.busy = Program.busy.AddBit(type);
            }
            else
            {
                Program.busy = Program.busy.RemoveBit(type);
            }
            return Program.busy > 0;
        }

        public static void Main()
        {

            Debug.Print("Starting..");

#if DEBUG
            Logger.Logged += Logger_Logged;
            Logger.Info("Logger inited");
#endif

            try
            {
                Init();
                #if DEBUG
                Logger.Info("Init test..");
                InitTest();
                #endif
                Debug.EnableGCMessages(false);
                Logger.Info("Started!");

                /*fakeLogsTimer = new Timer(s =>
                {
                    var m = Bordmonitor.ShowText("Hello world!", BordmonitorFields.Item, 1, true);
                    //m.ReceiverDescription = null;
                    Manager.ProcessMessage(m);
                }, null, 0, 500);*/

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                LED.Write(true);
                Logger.Error(ex, "while modules initialization");
            }
        }

        static void Logger_Logged(LoggerArgs args)
        {
            if (args.Priority == Tools.LogPriority.Error)
            {
                // store errors to arraylist
                error = true;
                RefreshLEDs();
            }
            Debug.Print(args.LogString);
        }

        /*static void Init2()
       {
           LED = new OutputPort(Pin.PA8, false);

           var player = new BluetoothOVC3860(Serial.COM2);
           player.IsCurrentPlayer = true;
           player.PlayerHostState = PlayerHostState.On;
           player.IsPlayingChanged += (p, value) => LED.Write(value);
            
           //Button.OnPress(Pin.PC1, player.PlayPauseToggle);
           //Button.OnPress(Pin.PC2, player.Next);
           //Button.OnPress(Pin.PC3, player.Prev);

           LED.Write(true);
       }*/

       class Button
       {
           static ArrayList buttons = new ArrayList();

           public delegate void Action();

           public static void OnPress(Cpu.Pin pin, Action callback)
           {
               InterruptPort btn = new InterruptPort(pin, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeLow);
               btn.OnInterrupt += (s, e, t) => callback();
               buttons.Add(btn);
           }
       }
    }
}
