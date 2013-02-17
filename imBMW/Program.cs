using GHIElectronics.NETMF.FEZ;
using imBMW.Multimedia;
using imBMW.Tools;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace imBMW
{
    public class Program
    {
        static void Init()
        {
            iBus.Manager.Init(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);
            iBus.Devices.CDChanger.Init(new iPodViaHeadset((Cpu.Pin)FEZ_Pin.Digital.Di3));
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
