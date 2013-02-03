using GHIElectronics.NETMF.FEZ;
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
        public static void Main()
        {
            Debug.Print("Starting..");

            iBus.Manager.Init(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);
            iBus.Devices.iPodChanger.Init((Cpu.Pin)FEZ_Pin.Digital.Di3);
            
            Debug.Print("Started!");

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
