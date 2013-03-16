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
        static bool showSpeedRpm = false;

        static void Init()
        {
            // Create serial port to work with Melexis TH3122
            ISerialPort iBusPort = new SerialPortTH3122(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);

            InputPort jumper = new InputPort((Cpu.Pin)FEZ_Pin.Digital.An7, false, Port.ResistorMode.PullUp);
            if (!jumper.Read())
            {
                Logger.Info("Jumper installed. Starting virtual COM port.");

                // Init hub between iBus port and virtual USB COM port
                USBC_CDC cdc = USBClientController.StandardDevices.StartCDC_WithDebugging();
                iBusPort = new SerialPortHub(iBusPort, new SerialPortCDC(cdc));
            }

            // Enable iBus Manager
            iBus.Manager.Init(iBusPort);
            Logger.Info("iBus manager inited");
            
            iBus.Manager.AfterMessageReceived += (e) =>
            {
                // Show only processed events
                //if (e.Message.ReceiverDescription == null) { return; }
                Logger.Info(e.Message);
            };
            iBus.Manager.AfterMessageSent += (e) =>
            {
                Logger.Info("Sent: " + e.Message.ToString());
            };
            Logger.Info("iBus manager logger events subscribed");

            // Set iPod via headset as CD-Changer emulator
            iBus.Devices.CDChanger.Init(new Multimedia.iPodViaHeadset((Cpu.Pin)FEZ_Pin.Digital.Di3));
            Logger.Info("CD-Changer inited");

            // Enable comfort features
            //Features.Comfort.AllFeaturesEnabled = true;
            Features.Comfort.AutoLockDoors = true;
            Features.Comfort.AutoUnlockDoors = true;
            Logger.Info("Comfort features inited");
            
            // Example features:
            iBus.Manager.AddMessageReceiverForDestinationDevice(iBus.DeviceAddress.CDChanger, (m) =>
            {
                // "Change CD" message from radio to CDC: 38 06 XX
                if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x06)
                {
                    byte cdNumber = m.Data[2];
                    switch (cdNumber)
                    {
                        case 0x06:
                            // CD6 button: Open trunk when ignition isn't off and not driving
                            if (InstrumentClusterElectronics.CurrentIgnitionState != IgnitionState.Off)
                            {
                                if (InstrumentClusterElectronics.CurrentSpeed == 0)
                                {
                                    BodyModule.OpenTrunk();
                                    Radio.DisplayTextWithDelay("Trunk open", TextAlign.Center);
                                }
                                else
                                {
                                    Radio.DisplayTextWithDelay("Stop car", TextAlign.Center);
                                }
                            }
                            else
                            {
                                Radio.DisplayTextWithDelay("Turn on ign", TextAlign.Center);
                            }
                            break;
                        case 0x05:
                            // CD5 button: Toggle current speed and RPM showing on radio display
                            showSpeedRpm = !showSpeedRpm;
                            if (showSpeedRpm)
                            {
                                ShowSpeedRPM(InstrumentClusterElectronics.CurrentSpeed, InstrumentClusterElectronics.CurrentRPM);
                            }
                            else
                            {
                                Radio.DisplayTextWithDelay("Speed off", TextAlign.Center);
                            }
                            break;
                        case 0x04:
                            BodyModule.CloseWindows();
                            break;
                        case 0x03:
                            BodyModule.OpenWindows();
                            break;
                    }
                    m.ReceiverDescription = "Change CD" + cdNumber;
                }
            });
            Logger.Info("Custom features inited");

            InstrumentClusterElectronics.SpeedRPMChanged += (e) =>
            {
                if (showSpeedRpm)
                {
                    ShowSpeedRPM(e.Speed, e.RPM);
                }
            };
            Logger.Info("Events subscribed");
        }

        static void ShowSpeedRPM(uint speed, uint rpm)
        {
            string s = speed.ToString();
            while (s.Length < 3)
            {
                s = (char)0x19 + s;
            }
            Radio.DisplayTextWithDelay(s + "kmh " + rpm);
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
