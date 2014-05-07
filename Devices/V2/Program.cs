﻿using GHI.OSHW.Hardware;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System;
using System.IO.Ports;
using System.Threading;
using GHI.Hardware.FEZCerb;
using imBMW.Multimedia;
using System.Collections;
using System.Text;
using imBMW.Tools;
using Microsoft.SPOT.IO;
using System.IO;
using imBMW.Features.Menu;
using imBMW.iBus.Devices.Emulators;
using imBMW.Features.Menu.Screens;

namespace imBMW.Devices.V2
{
    public class Program
    {
        static OutputPort LED;
        static OutputPort ShieldLED;

        static void Init2()
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
        }


















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

        static IAudioPlayer player;

        static void Init()
        {
            LED = new OutputPort(Pin.PA8, false);

            var version = "HW V2, FW V1.0";
            SettingsScreen.Instance.Status = version;
            Logger.Info(version);

            var sd = GetRootDirectory();

            // todo get config

            // todo log to sd if logging turned on

            // Create serial port to work with Melexis TH3122
            ISerialPort iBusPort = new SerialPortTH3122(Serial.COM3, Pin.PC2, true);
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

            // Enable iBus Manager
            iBus.Manager.Init(iBusPort);
            Logger.Info("iBus manager inited");

            Message sent1 = null, sent2 = null; // light "buffer" for last 2 messages
            bool isSent1 = false;
            iBus.Manager.BeforeMessageReceived += (e) =>
            {
                LED.Write(Busy(true, 1));
            };
            iBus.Manager.AfterMessageReceived += (e) =>
            {
                LED.Write(Busy(false, 1));
#if DEBUG
                // Show only messages which are described
                //if (e.Message.Describe() == null) { return; }
                // Filter CDC emulator messages echoed by iBus
                //if (e.Message.SourceDevice == iBus.DeviceAddress.CDChanger) { return; }
                if (e.Message.SourceDevice != DeviceAddress.Radio
                    && e.Message.DestinationDevice != DeviceAddress.Radio
                    && e.Message.SourceDevice != DeviceAddress.GraphicsNavigationDriver
                    && e.Message.SourceDevice != DeviceAddress.Diagnostic && e.Message.DestinationDevice != DeviceAddress.Diagnostic)
                {
                    return;
                }
                if (e.Message.ReceiverDescription == null)
                {
                    if (sent1 != null && sent1.Data.Compare(e.Message.Data))
                    {
                        e.Message.ReceiverDescription = sent1.ReceiverDescription;
                    }
                    else if (sent2 != null && sent2.Data.Compare(e.Message.Data))
                    {
                        e.Message.ReceiverDescription = sent2.ReceiverDescription;
                    }
                }
                Logger.Info(e.Message, "< ");
                /*if (e.Message.ReceiverDescription == null)
                {
                    Logger.Info(ASCIIEncoding.GetString(e.Message.Data));
                }*/
                //Logger.Info(e.Message.PacketDump);
#endif
            };
            iBus.Manager.BeforeMessageSent += (e) =>
            {
                LED.Write(Busy(true, 2));
            };
            iBus.Manager.AfterMessageSent += (e) =>
            {
                LED.Write(Busy(false, 2));
#if DEBUG
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
#endif
            };
            Logger.Info("iBus manager events subscribed");

            // Enable comfort features
            //Features.Comfort.AllFeaturesEnabled = true;
            Features.Comfort.AutoLockDoors = true;
            Features.Comfort.AutoUnlockDoors = true;
            Features.Comfort.AutoCloseWindows = true;
            Logger.Info("Comfort features inited");

            // Set iPod or Bluetooth as AUX or CDC-emulator
            player = new BluetoothOVC3860(Serial.COM2, sd != null ? sd + @"\contacts.vcf" : null);
            //player = new iPodViaHeadset(Pin.PC2);
            
            Radio.Init();
            Logger.Info("Radio inited");
            if (Manager.FindDevice(DeviceAddress.OnBoardMonitor))
            {
                MediaEmulator emulator;
                emulator = new BordmonitorAUX(player);
                //emulator = new CDChanger(player);
                //MenuScreen.MaxItemsCount = 6;
                //Bordmonitor.MK2Mode = true;
                BordmonitorMenu.Init(emulator);
                Logger.Info("BordmonitorAUX inited");
            }
            else
            {
                // TODO implement radio menu
                //iBus.Devices.Emulators.CDChanger.Init(player);
                Logger.Info("CDChanger emulator inited");
            }
            ShieldLED = new OutputPort(Pin.PA7, false);
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

            //SampleFeatures.Init();
            //Logger.Info("Sample features inited");

            /*blinkerTimer = new Timer((s) =>
            {
                if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off && !blinkerOn)
                {
                    return;
                }
                blinkerOn = !blinkerOn;
                RefreshLEDs();
            }, null, 0, 3000);*/

            LED.Write(true);
            Thread.Sleep(50);
            LED.Write(false);
            Logger.Info("LED blinked - inited");
        }

        static void RefreshLEDs()
        {
            byte b = 0;
            if (error)
            {
                b = b.AddBit(0);
            }
            if (blinkerOn)
            {
                //b = b.AddBit(2);
            }
            if (player.IsPlaying)
            {
                b = b.AddBit(4);
            }
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, b));
        }

        public static string GetRootDirectory()
        {
            try
            {
                Logger.Info("Mount", "SD");
                GHI.OSHW.Hardware.StorageDev.MountSD();
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

    }
}
