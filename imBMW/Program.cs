using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using System.IO.Ports;
using System.Text;

namespace imBMW
{
    public class Program
    {
        //static OutputPort iPod;
        static OutputPort LED;

        public static void Main()
        {
            Debug.Print("Start");

            LED = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.LED, true);
            //iPod = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di3, true);

            iBus.Manager.Init(Serial.COM3, (Cpu.Pin)FEZ_Pin.Interrupt.Di4);
            iBus.Devices.iPodChanger.Init((Cpu.Pin)FEZ_Pin.Digital.Di3);

            /*InterruptPort btn = new InterruptPort((Cpu.Pin)FEZ_Pin.Interrupt.LDR, true,
                Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            btn.OnInterrupt += new NativeEventHandler(btn_OnInterrupt);*/

            /*InterruptPort sensta = new InterruptPort((Cpu.Pin)FEZ_Pin.Interrupt.Di4, true,
                Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            sensta.OnInterrupt += new NativeEventHandler(sensta_OnInterrupt);*/

            Debug.Print("OK");

            Thread t = new Thread(test);
            t.Start();

            Thread.Sleep(Timeout.Infinite);
        }

        static void test()
        {
            iBus.Devices.CarOutputDevices.WriteRadioText("Hello");
            int i = 0;
            while (i < 100)
            {
                iBus.Devices.CarOutputDevices.WriteRadioText(i.ToString(), iBus.Devices.CarOutputDevices.TextAlign.Right);
                i++;
                //Thread.Sleep(1000);
            }
        }

        static void sensta_OnInterrupt(uint port, uint state, DateTime time)
        {
            LED.Write(!LED.Read());
        }

        static void btn_OnInterrupt(uint port, uint state, DateTime time)
        {
            //iPod.Write(state == 0);
            LED.Write(state == 0);
        }

    }
}
