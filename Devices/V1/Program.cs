using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.USBClient;
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
using System;
using System.IO.Ports;
using System.Threading;

namespace imBMW.Devices.V1
{
    public class Program
    {
        /**
         *
         * WARNING!
         *
         * Change Target framework version of imBMW project to 4.1
         * to use it with original imBMW V1 device based on FEZ Mini
         *
         */

        const string version = "HW2 FW1.0.2";

        static OutputPort LED;
        static IAudioPlayer player;

        static void Init()
        {
            LED = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.LED, false);

            var settings = Settings.Init(null);
            var log = settings.Log || settings.LogToSD;

            // TODO move to settings
            Localization.Current = new EnglishLocalization();
            Features.Comfort.AutoLockDoors = true;
            Features.Comfort.AutoUnlockDoors = true;
            Features.Comfort.AutoCloseWindows = true;
            Logger.Info("Preferences inited");

            Logger.Info(version);
            SettingsScreen.Instance.Status = version;

            // Create serial port to work with Melexis TH3122
            ISerialPort iBusPort = new SerialPortTH3122(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);
            //ISerialPort iBusPort = new SerialPortOptoPair(Serial.COM3);
            Logger.Info("TH3122 serial port inited");

            InputPort jumper = new InputPort((Cpu.Pin)FEZ_Pin.Digital.An7, false, Port.ResistorMode.PullUp);
            if (!jumper.Read())
            {
                Logger.Info("Jumper installed. Starting virtual COM port");

                // Init hub between iBus port and virtual USB COM port
                ISerialPort cdc = new SerialPortCDC(USBClientController.StandardDevices.StartCDC_WithDebugging(), 0, iBus.Message.PacketLengthMax);
                iBusPort = new SerialPortHub(iBusPort, cdc);
                Logger.Info("Serial port hub started");
            }

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
                if (settings.LogMessageToASCII)
                {
                    Logger.Info(e.Message.ToPrettyString(true, true), "< ");
                }
                else
                {
                    Logger.Info(e.Message, "< ");
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

            // Set iPod or Bluetooth as AUX or CDC-emulator for Bordmonitor or Radio
            //player = new BluetoothOVC3860(Serial.COM2, sd != null ? sd + @"\contacts.vcf" : null);
            player = new iPodViaHeadset((Cpu.Pin)FEZ_Pin.Digital.Di3);

            if (settings.MenuMode != Tools.MenuMode.RadioCDC || Manager.FindDevice(DeviceAddress.OnBoardMonitor))
            {
                MediaEmulator emulator;
                if (settings.MenuMode == Tools.MenuMode.BordmonitorCDC)
                {
                    emulator = new CDChanger(player);
                    if (settings.MenuModeMK2)
                    {
                        Bordmonitor.MK2Mode = true;
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                    }
                }
                else
                {
                    emulator = new BordmonitorAUX(player);
                }
                //MenuScreen.MaxItemsCount = 6;
                BordmonitorMenu.Init(emulator);
                Logger.Info("Bordmonitor menu inited");
            }
            else
            {
                Localization.Current = new RadioLocalization();
                SettingsScreen.Instance.CanChangeLanguage = false;
                Radio.Init();
                RadioMenu.Init(new CDChanger(player));
                Logger.Info("Radio menu inited");
            }

            LED.Write(true);
            Thread.Sleep(50);
            LED.Write(false);
            Logger.Info("LED blinked - inited");
        }

        static byte busy = 0;

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
                Logger.Error(ex, "while modules initialization");
            }
        }

        static void Logger_Logged(LoggerArgs args)
        {
            Debug.Print(args.LogString);
        }
    }
}
