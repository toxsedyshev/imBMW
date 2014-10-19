using System;
using Microsoft.SPOT;
using System.Collections;
using Microsoft.SPOT.Hardware;

namespace imBMW.Shields
{
    public static class ShieldManager
    {
        public static Hashtable Ports { get; private set; }

        public static void Init(Hashtable ports)
        {
            Ports = ports;
        }

        public static Cpu.Pin GetPin(this ShieldPort port)
        {   
            return (Cpu.Pin)Ports[port];
        }

        /*public static Cpu.AnalogChannel GetAnalogChannel(this ShieldPort port)
        {
            return null;// TODO 4.1 (Cpu.AnalogChannel)Ports[port];
        }

        public static ArrayList InitShields(ShieldPort detectPort)
        {
            return InitShields(detectPort.GetAnalogChannel());
        }

        public static ArrayList InitShields(Cpu.AnalogChannel detectPin)
        {
            ArrayList shields = new ArrayList();
            var port = new AnalogInput(detectPin, 3300, 0, 12);
            var val = port.Read();
            if (val == BluetoothOVC3860Shield.DetectValue)
            {
                shields.Add(new BluetoothOVC3860Shield());
            }
            return shields;
        }*/
    }
}
