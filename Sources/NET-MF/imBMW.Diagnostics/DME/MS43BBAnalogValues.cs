using System;
using imBMW.iBus;
using System.Text;
using imBMW.Tools;

namespace imBMW.Diagnostics.DME
{
    public class MS43BBAnalogValues : MS43AnalogValues
    {
        public MS43BBAnalogValues() { }

        public MS43BBAnalogValues(Message message)
            : base(message)
        { }

        public MS43BBAnalogValues(Message message, DateTime loggerStarted)
            : base(message, loggerStarted)
        { }

        public static new bool CanParse(Message message)
        {
            return message.SourceDevice == DeviceAddress.DME
                && message.DestinationDevice == DeviceAddress.Diagnostic
                && message.Data.Length == 51
                && message.Data[0] == 0xA0;
        }

        protected override bool IsValidMessage(Message message)
        {
            return CanParse(message);
        }

        public override void Parse(Message message)
        {
            base.Parse(message);
            var d = message.Data;
            WideBandLambda = (double)d[42] / 255 + 0.5; // AFR=7.35..22.05 with 0.057 step
            IntakePressure = d[43] * 10; // 0..2550 hPa = 0..2.55bar with 0.01bars step
            FuelPressure = d[44] * 40; // 0..10.2bar with 0.04bars step VV
            OilPressure = d[45] * 40;
            IntakeTempAfterCooler = d[46] / 2.55; // 0..100C with 0.39C step VV
            CoolerInTemp = d[47] / 2.55;
            CoolerOutTemp = d[48] / 2.55;
            IsMethanolInjecting = d[49] == 1;
            IsMethanolFailsafe = d[50] == 1;
        }

        public static DBusMessage ModifyMS43Message(DMEAnalogValues av, Message message)
        {
            var data = message.Data.PadRight(0x00, 9);
            data[42] = ToByte((av.WideBandLambda - 0.5) * 255);
            data[43] = ToByte(av.IntakePressure / 10);
            data[44] = ToByte(av.FuelPressure / 40);
            data[45] = ToByte(av.OilPressure / 40);
            data[46] = ToByte(av.IntakeTempAfterCooler * 2.55);
            data[47] = ToByte(av.CoolerInTemp * 2.55);
            data[48] = ToByte(av.CoolerOutTemp * 2.55);
            data[49] = ToByte(av.IsMethanolInjecting ? 0x01 : 0x00);
            data[50] = ToByte(av.IsMethanolFailsafe ? 0x01 : 0x00);
            return new DBusMessage(DeviceAddress.DME, message.ReceiverDescription, data);
        }

        static byte ToByte(int d)
        {
            return (byte)Math.Min(255, Math.Max(0, d));
        }

        static byte ToByte(double d)
        {
            return (byte)Math.Min(255, Math.Max(0, Math.Round(d)));
        }
    }
}
