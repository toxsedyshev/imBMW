using GHIElectronics.NETMF.FEZ;
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
            // Enable iBus Manager to work with Melexis TH3122
            iBus.Manager.Init(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);

            // Set iPod via headset as CD-Changer emulator
            iBus.Devices.CDChanger.Init(new Multimedia.iPodViaHeadset((Cpu.Pin)FEZ_Pin.Digital.Di3));

            // Enable all comfort features
            Features.Comfort.AllFeaturesEnabled = true;

            // Example features:
            iBus.Manager.AddMessageReceiverForDestinationDevice(iBus.DeviceAddress.CDChanger, (m) =>
            {
                // "Change CD" message from radio to CDC: 38 06 XX
                if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x06)
                {
                    switch (m.Data[2])
                    {
                        case 0x06:
                            // CD6 button: Open trunk when ignition isn't off and not driving
                            if (InstrumentClusterElectronics.CurrentSpeed == 0
                                && InstrumentClusterElectronics.CurrentIgnitionState != IgnitionState.Off)
                            {
                                BodyModule.OpenTrunk();
                                Radio.DisplayTextWithDelay("Trunk open", TextAlign.Center);
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
                    }
                }
            });
            InstrumentClusterElectronics.SpeedRPMChanged += (e) =>
            {
                if (showSpeedRpm)
                {
                    ShowSpeedRPM(e.Speed, e.RPM);
                }
            };
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
