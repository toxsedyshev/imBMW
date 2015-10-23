using System;
using imBMW.iBus;
using Microsoft.SPOT;
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
            AFR = (d[30] * 0.0009914 - 31.98155) * 14.7;
            WideBandLambda = d[30] * 0.0009914 - 31.98155;
        }
    }
}
