using System;
using Microsoft.SPOT;
using GHIElectronics.NETMF.FEZ;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;
using System.IO.Ports;
using System.Threading;

namespace imBMW.Devices.V1
{
    public class Program
    {
        static byte[] DataDisk6 = new byte[] { 0x38, 0x06, 0x06 };

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
                    && iBus.Devices.Real.InstrumentClusterElectronics.CurrentIgnitionState == iBus.Devices.Real.IgnitionState.On)
                {
                    iBus.Devices.Real.Doors.OpenTrunk();
                    Logger.Info("Trunk opened");
                }
            });
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
