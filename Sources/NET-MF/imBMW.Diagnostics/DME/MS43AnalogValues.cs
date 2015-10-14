using System;
using Microsoft.SPOT;
using imBMW.iBus;

namespace imBMW.Diagnostics.DME
{
    public class MS43AnalogValues : DMEAnalogValues
    {
        public MS43AnalogValues() { }

        public MS43AnalogValues(Message message)
        {
            Parse(message);
        }

        public static bool Check(Message message)
        {
            return message is DBusMessage
                && message.SourceDevice == DeviceAddress.DME
                && message.Data.Length == 42 
                && message.Data[0] == 0xA0;
        }

        public MS43AnalogValues Parse(Message message)
        {
            if (!Check(message))
            {
                throw new Exception("Not MS43 analog values message");
            }
            var d = message.Data;
            this.RPM = (d[1] << 8) + d[2];
            this.Speed = d[3];
            return this;
        }
    }
}
