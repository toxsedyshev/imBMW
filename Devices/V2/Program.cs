using GHI.OSHW.Hardware;
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
            player.IsPlayerHostActive = true;
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
                //if (e.Message.SourceDevice != DeviceAddress.Radio
                //    && e.Message.DestinationDevice != DeviceAddress.Radio
                //    && e.Message.SourceDevice != DeviceAddress.GraphicsNavigationDriver)
                if (e.Message.SourceDevice != DeviceAddress.Diagnostic && e.Message.DestinationDevice != DeviceAddress.Diagnostic)
                {
                    return;
                }
                Logger.Info(e.Message, "<<");
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
                Logger.Info(e.Message, ">>");
#endif
            };
            Logger.Info("iBus manager events subscribed");

            // Set iPod or Bluetooth as AUX or CDC-emulator
            player = new BluetoothOVC3860(Serial.COM2, sd != null ? sd + @"\contacts.vcf" : null);
            
            // TODO remove
            var contacts = (player as BluetoothOVC3860).GetContacts(13, 10);

            if (Manager.FindDevice(DeviceAddress.OnBoardMonitor))
            {
                iBus.Devices.Emulators.BordmonitorAUX.Init(player); //new Multimedia.iPodViaHeadset(Pin.PC2));
                Logger.Info("BordmonitorAUX inited");

                // TODO remove to features
                BodyModule.UpdateBatteryVoltage();
                Manager.AddMessageReceiverForSourceDevice(DeviceAddress.OnBoardMonitor, (m) =>
                {
                    if (!player.IsEnabled)
                    {
                        return;
                    }
                    if (m.Data[0] == 0x48 && m.Data[1] == 0x87) // TODO move to bordmonitor menu
                    {
                        new Thread(() =>
                        {
                            BodyModule.UpdateBatteryVoltage();
                            Thread.Sleep(500);
                            Bordmonitor.ShowText("Скорость:   " + InstrumentClusterElectronics.CurrentSpeed + "км/ч", BordmonitorFields.Item, 0);
                            Bordmonitor.ShowText("Обороты:    " + InstrumentClusterElectronics.CurrentRPM, BordmonitorFields.Item, 1);
                            Bordmonitor.ShowText("Двигатель:  " + InstrumentClusterElectronics.TemperatureCoolant + "°C", BordmonitorFields.Item, 2);
                            //Bordmonitor.ShowText("Улица:      " + InstrumentClusterElectronics.TemperatureOutside + "°C", BordmonitorFields.Item, 3);
                            Bordmonitor.ShowText("Напряжение: " + BodyModule.BatteryVoltage + "В", BordmonitorFields.Item, 3);
                            for (int i = 4; i < 10; i++)
                            {
                                Bordmonitor.ShowText("", BordmonitorFields.Item, i);
                            }
                            Bordmonitor.RefreshScreen();
                        }).Start();
                    }
                    if (m.Data[0] == 0x48 && m.Data[1] == 0x88) // TODO move to bordmonitor menu
                    {
                        new Thread(() =>
                        {
                            int i = 0;
                            foreach (var c in contacts)
                            {
                                Bordmonitor.ShowText((c as PhoneContact).Name, BordmonitorFields.Item, i++);
                            }
                            Bordmonitor.RefreshScreen();
                        }).Start();
                    }
                });
            }
            else
            {
                iBus.Devices.Emulators.CDChanger.Init(player); //new Multimedia.iPodViaHeadset(Pin.PC2));
                Logger.Info("CDChanger emulator inited");
            }
            ShieldLED = new OutputPort(Pin.PA7, false);
            player.IsPlayingChanged += (p, s) =>
            {
                ShieldLED.Write(s);
                RefreshLEDs();
            };
            Logger.Info("Player events subscribed");

            // Enable comfort features
            //Features.Comfort.AllFeaturesEnabled = true;
            Features.Comfort.AutoLockDoors = true;
            Features.Comfort.AutoUnlockDoors = true;
            Features.Comfort.AutoCloseWindows = true;
            Logger.Info("Comfort features inited");

            SampleFeatures.Init();
            Logger.Info("Sample features inited");

            /*iBus.Manager.AddMessageReceiverForSourceDevice(iBus.DeviceAddress.OnBoardMonitor, (m) =>
            {
                if (m.Data[0] == 0x48 && m.Data[1] == 0x81)
                {
                    BodyModule.LockDoors();
                    //iBus.Manager.EnqueueMessage(new iBus.Message(iBus.DeviceAddress.CDChanger, iBus.DeviceAddress.Broadcast, 0x02, 0x01),
                    //    new iBus.Message(iBus.DeviceAddress.CDChanger, iBus.DeviceAddress.Broadcast, 0x02, 0x00));
                    //Thread.Sleep(100);
                    //iBus.Manager.EnqueueMessage(new iBus.Message(iBus.DeviceAddress.OnBoardMonitor, iBus.DeviceAddress.GraphicsNavigationDriver, 0x49, 0x01));
                    //iBus.Manager.EnqueueMessage(new iBus.Message(iBus.DeviceAddress.Radio, iBus.DeviceAddress.GraphicsNavigationDriver, 0x23, 0x62, 0x30, (byte)'A', (byte)'B'));
                }
            });*/

            /*ISerialPort iBusPort2 = new SerialPortTH3122(Serial.COM2, Pin.PC1, true);
            while (true)
            {

                Thread.Sleep(500);
                iBusPort2.Write(new iBus.Message(iBus.DeviceAddress.Radio, iBus.DeviceAddress.GraphicsNavigationDriver, 0x23, 0x62, 0x30, (byte)'A', (byte)'B').Packet);
                //iBus.Manager.EnqueueMessage(new iBus.Message(iBus.DeviceAddress.Radio, iBus.DeviceAddress.GraphicsNavigationDriver, 0x23, 0x62, 0x30, (byte)'A', (byte)'B'));
            }*/

            blinkerTimer = new Timer((s) =>
            {
                if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off && !blinkerOn)
                {
                    return;
                }
                blinkerOn = !blinkerOn;
                RefreshLEDs();
            }, null, 0, 3000);

            //TestFlash();

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
                b = b.AddBit(2);
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
                    Logger.Error("Not formatted!", "SD");
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
            }
            Debug.Print(args.LogString);
        }

    }
}
