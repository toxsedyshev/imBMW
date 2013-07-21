using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace imBMW.Devices.V1
{
    static class SampleFeatures
    {
        static bool showSpeedRpm = false;
        static DateTime lastMessage = DateTime.Now;
        static TimeSpan maxMessagesSpan = TimeSpan.Zero;

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
                            Radio.DisplayTextWithDelay("Close winds");
                            BodyModule.CloseWindows();
                            break;
                        case 0x03:
                            Features.Comfort.NextComfortCloseEnabled = !Features.Comfort.NextComfortCloseEnabled;
                            Radio.DisplayTextWithDelay("[" + (Features.Comfort.NextComfortCloseEnabled ? "X" : " ") + "]Comf cls");
                            break;
                        case 0x02:
                            Radio.DisplayTextWithDelay(DateTime.Now.ToString("ddHH") + " " + maxMessagesSpan.GetTotalMinutes() + "\"" + maxMessagesSpan.Seconds); // dd HH:mm:ss
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

            iBus.Manager.AfterMessageReceived += (m) =>
            {
                var now = DateTime.Now;
                var span = now - lastMessage;
                lastMessage = now;
                if (span > maxMessagesSpan)
                {
                    maxMessagesSpan = span;
                }
            };

            /*InstrumentClusterElectronics.IgnitionStateChanged += (e) =>
            {
                if (e.CurrentIgnitionState != IgnitionState.Off && e.PreviousIgnitionState == IgnitionState.Off)
                {
                    iBus.Manager.EnqueueMessage(new iBus.Message(iBus.DeviceAddress.Diagnostic, iBus.DeviceAddress.GlobalBroadcastAddress, 0x0c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x06));
                }
            };*/
        }

        static void ShowSpeedRPM(uint speed, uint rpm, bool delay = false)
        {
            string s = speed.ToString().PrependToLength((char)0x19, 3) + "kmh " + rpm;
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
