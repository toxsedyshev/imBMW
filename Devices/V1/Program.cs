using System;
using Microsoft.SPOT;
using GHIElectronics.NETMF.FEZ;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using System.IO.Ports;
using System.Threading;

namespace imBMW.Devices.V1
{
    public class Program
    {
        const uint doorsLockSpeed = 5;

        static byte[] DataDisk3 = new byte[] { 0x38, 0x06, 0x03 };
        static byte[] DataDisk4 = new byte[] { 0x38, 0x06, 0x04 };
        static byte[] DataDisk5 = new byte[] { 0x38, 0x06, 0x05 };
        static byte[] DataDisk6 = new byte[] { 0x38, 0x06, 0x06 };

        static bool showSpeedRpm = false;
        static bool doorsLocked = false;

        static void Init()
        {
            iBus.Manager.AddMessageReceiverForSourceDevice(iBus.DeviceAddress.Radio, (m) =>
            {
                Logger.Info(m);
            });

            iBus.Manager.Init(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);
            iBus.Devices.CDChanger.Init(new Multimedia.iPodViaHeadset((Cpu.Pin)FEZ_Pin.Digital.Di3));

            iBus.Manager.AddMessageReceiverForDestinationDevice(iBus.DeviceAddress.CDChanger, (m) =>
            {
                if (m.Data.Compare(DataDisk6)
                    && InstrumentClusterElectronics.CurrentSpeed == 0
                    && InstrumentClusterElectronics.CurrentIgnitionState != IgnitionState.Off)
                {
                    BodyModule.OpenTrunk();
                    Radio.DisplayTextWithDelay("Trunk open", TextAlign.Center);
                }
                else if (m.Data.Compare(DataDisk5))
                {
                    showSpeedRpm = !showSpeedRpm;
                    if (showSpeedRpm)
                    {
                        ShowSpeedRPM(InstrumentClusterElectronics.CurrentSpeed, InstrumentClusterElectronics.CurrentRPM);
                    }
                    else
                    {
                        Radio.DisplayTextWithDelay("Speed off", TextAlign.Center);
                    }
                }
                else if (m.Data.Compare(DataDisk4))
                {
                    //Doors.LockDoors();
                }
                else if (m.Data.Compare(DataDisk3))
                {
                    //Doors.UnlockDoors();
                }
            });
            InstrumentClusterElectronics.SpeedRPMChanged += (e) =>
            {
                if (!doorsLocked && e.Speed > doorsLockSpeed)
                {
                    BodyModule.LockDoors();
                    doorsLocked = true;
                }
                if (showSpeedRpm)
                {
                    ShowSpeedRPM(e.Speed, e.RPM);
                }
            };
            InstrumentClusterElectronics.IgnitionStateChanged += (e) =>
            {
                if (doorsLocked && e.CurrentIgnitionState == IgnitionState.Off)
                {
                    BodyModule.UnlockDoors();
                    doorsLocked = false;
                }
            };
            BodyModule.RemoteKeyButtonPressed += (e) =>
            {
                if (e.Button == RemoteKeyButton.Lock)
                {
                    BodyModule.CloseWindows();
                    BodyModule.CloseSunroof();
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
