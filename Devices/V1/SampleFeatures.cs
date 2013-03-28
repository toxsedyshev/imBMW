using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;

namespace imBMW.Devices.V1
{
    static class SampleFeatures
    {
        static bool showSpeedRpm = false;

        public static void Init()
        {
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
                                ShowSpeedRPM(InstrumentClusterElectronics.CurrentSpeed, InstrumentClusterElectronics.CurrentRPM, true);
                            }
                            else
                            {
                                Radio.DisplayTextWithDelay("Speed off", TextAlign.Center);
                            }
                            break;
                        case 0x04:
                            BodyModule.CloseWindows();
                            Radio.DisplayText("Close winds");
                            break;
                        case 0x03:
                            BodyModule.OpenWindows();
                            Radio.DisplayText("Open winds");
                            break;
                        case 0x02:
                            Radio.DisplayTextWithDelay(DateTime.Now.ToString("dd HH:mm:ss"));
                            break;
                        case 0x01:
                            Radio.DisplayTextWithDelay(":)", TextAlign.Center);
                            break;
                    }
                    m.ReceiverDescription = "Change CD" + cdNumber;
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

        static void ShowSpeedRPM(uint speed, uint rpm, bool delay = false)
        {
            string s = speed.ToString();
            while (s.Length < 3)
            {
                s = (char)0x19 + s;
            }
            s += "kmh " + rpm;
            if (delay)
            {
                Radio.DisplayTextWithDelay(s);
            }
            else
            {
                Radio.DisplayText(s);
            }
        }
    }
}
