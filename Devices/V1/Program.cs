using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.USBClient;
using imBMW.iBus.Devices.Real;
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
        static void Init()
        {
            OutputPort LED = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.LED, false);

            // Create serial port to work with Melexis TH3122
            ISerialPort iBusPort = new SerialPortTH3122(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);
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

            // Enable iBus Manager
            iBus.Manager.Init(iBusPort);
            Logger.Info("iBus manager inited");

            iBus.Manager.BeforeMessageReceived += (e) =>
            {
                LED.Write(true);
            };
            iBus.Manager.AfterMessageReceived += (e) =>
            {
                LED.Write(false);
                #if DEBUG
                // Show only messages which are described
                //if (e.Message.Describe() == null) { return; }
                // Filter CDC emulator messages echoed by iBus
                //if (e.Message.SourceDevice == iBus.DeviceAddress.CDChanger) { return; }
                Logger.Info(e.Message, "<<");
                #endif
            };
            iBus.Manager.BeforeMessageSent += (e) =>
            {
                LED.Write(true);
            };
            iBus.Manager.AfterMessageSent += (e) =>
            {
                LED.Write(false);
                #if DEBUG
                Logger.Info(e.Message, ">>");
                #endif
            };
            Logger.Info("iBus manager events subscribed");
            
            // Set iPod via headset as CD-Changer emulator
            iBus.Devices.CDChanger.Init(new Multimedia.iPodViaHeadset((Cpu.Pin)FEZ_Pin.Digital.Di3));
            Logger.Info("CD-Changer inited");

            // Enable comfort features
            //Features.Comfort.AllFeaturesEnabled = true;
            Features.Comfort.AutoLockDoors = true;
            Features.Comfort.AutoUnlockDoors = true;
            Features.Comfort.AutoCloseWindows = true;
            Logger.Info("Comfort features inited");

            SampleFeatures.Init();
            Logger.Info("Sample features inited");

            LED.Write(true);
            Thread.Sleep(50);
            LED.Write(false);
            Logger.Info("LED blinked - inited");
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
