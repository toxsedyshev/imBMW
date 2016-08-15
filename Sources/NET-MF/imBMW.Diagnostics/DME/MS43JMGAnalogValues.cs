using System;
using imBMW.iBus;
using System.Text;
using imBMW.Tools;

namespace imBMW.Diagnostics.DME
{
    public class MS43JMGAnalogValues : MS43AnalogValues
    {
        public MS43JMGAnalogValues() { }

        public MS43JMGAnalogValues(Message message)
            : base(message)
        { }

        public MS43JMGAnalogValues(Message message, DateTime loggerStarted)
            : base(message, loggerStarted)
        { }

        public override void Parse(Message message)
        {
            base.Parse(message);
            var d = message.Data;
            LambdaHeatingAfterCats1 = -1; // byte #30 is used for AFR instead
            LambdaHeatingAfterCats2 = -1; // byte #31 is used for IntakePressure instead
            WideBandLambda = (double)d[30] / 255 + 0.5; // TODO fix - not JMG formula!
            AFR = WideBandLambda * 14.7;
            IntakePressure = d[31] * 10; // 0..2550 hPa
        }

        public static DBusMessage ModifyMS43Message(DMEAnalogValues av, Message message)
        {
            var data = message.Data.Skip(0);
            data[30] = ToByte((av.WideBandLambda - 0.5) * 255); // TODO why not? TBD with JMG
            data[31] = ToByte(av.IntakePressure / 10);
            return new DBusMessage(DeviceAddress.DME, message.ReceiverDescription, data);
        }

        static byte ToByte(double d)
        {
            return (byte)System.Math.Min(255, System.Math.Max(0, d));
        }
    }
}
