using System;
using imBMW.iBus;
using System.Text;
using imBMW.Tools;

namespace imBMW.Diagnostics.DME
{
    public class MS43AnalogValues : DMEAnalogValues
    {
        public MS43AnalogValues() { }

        public MS43AnalogValues(Message message)
        {
            Parse(message);
        }

        public MS43AnalogValues(Message message, DateTime loggerStarted)
            : base(loggerStarted)
        {
            Parse(message);
        }

        public static bool CanParse(Message message)
        {
            return message.SourceDevice == DeviceAddress.DME
                && message.DestinationDevice == DeviceAddress.Diagnostic
                && message.Data.Length == 42 
                && message.Data[0] == 0xA0;
        }

        public virtual void Parse(Message message)
        {
            if (!CanParse(message))
            {
                throw new Exception("Not MS43 analog values message");
            }
            var d = message.Data;
            RPM = (d[1] << 8) + d[2];
            Speed = d[3];
            Pedal = ((d[4] << 8) + d[5]) * 0.0018311;
            Throttle = ((d[6] << 8) + d[7]) * 0.0018311;
            AirMass = ((d[8] << 8) + d[9]) * 0.25;
            IntakeTemp = d[10] * 0.75 - 48;
            CoolantTemp = d[11] * 0.75 - 48;
            OilTemp = d[12] * 0.796 - 48;
            CoolantRadiatorTemp = d[13] * 0.75 - 48;
            IgnitionAngle = -d[14] * 0.375 + 72;
            InjectionTime = ((d[15] << 8) + d[16]) * 0.0053333;
            ISAPWM_IS = ((d[17] << 8) + d[18]) * 0.0015;
            ISAPWM_ISA = ((d[19] << 8) + d[20]) * 0.0015;
            VanosPositionIntake = d[21] * 0.375 + 60;
            VanosPositionExhaust = -d[22] * 0.375 - 60;
            VoltageKL15 = d[23] * 0.10156;
            LambdaIntegrator1 = ((d[24] << 8) + d[25]) * 0.000015259 + 0.5;
            LambdaIntegrator2 = ((d[26] << 8) + d[27]) * 0.000015259 + 0.5;
            LambdaHeatingBeforeCats1 = d[28] * 0.391;
            LambdaHeatingBeforeCats2 = d[29] * 0.391;
            LambdaHeatingAfterCats1 = d[30] * 0.391;
            LambdaHeatingAfterCats2 = d[31] * 0.391;
            AirMassPerStroke = ((d[32] << 8) + d[33]) * 0.0212;
            KnockSensor2 = ((d[34] << 8) + d[35]) * 0.0000778;
            KnockSensor5 = ((d[36] << 8) + d[37]) * 0.0000778;
            ElectricFanSpeed = d[38] * 0.39063;
            AtmosphericPressure = (int)(((d[39] << 8) + d[40]) * 0.08292);
            VoltageBattery = d[41] * 0.10156;
        }

        public override string GenerateLogString()
        {
            var s = new StringBuilder();
            //s.Append(Time); s.Append('+'); s.Append(Time.Millisecond); s.Append(";");
            s.Append(TimeSpan.GetTotalMilliseconds()); s.Append(";");
            s.Append(RPM); s.Append(";");
            s.Append(Speed); s.Append(";");
            s.Append(Throttle.ToString("F2")); s.Append(";");
            s.Append(Pedal.ToString("F2")); s.Append(";");
            s.Append(AFR.ToString("F3")); s.Append(";");
            s.Append(WideBandLambda.ToString("F5")); s.Append(";");
            s.Append(IntakePressure); s.Append(";");
            s.Append(AirMass.ToString("F2")); s.Append(";");
            s.Append(AirMassPerStroke.ToString("F4")); s.Append(";");
            s.Append(IgnitionAngle.ToString("F3")); s.Append(";");
            s.Append(InjectionTime.ToString("F5")); s.Append(";");
            s.Append(OilTemp.ToString("F2")); s.Append(";");
            s.Append(IntakeTemp.ToString("F2")); s.Append(";");
            s.Append(CoolantTemp.ToString("F2")); s.Append(";");
            s.Append(CoolantRadiatorTemp.ToString("F2")); s.Append(";");
            s.Append(ElectricFanSpeed.ToString("F1")); s.Append(";");
            s.Append(ISAPWM_IS.ToString("F2")); s.Append(";");
            s.Append(ISAPWM_ISA.ToString("F2")); s.Append(";");
            s.Append(KnockSensor2.ToString("F5")); s.Append(";");
            s.Append(KnockSensor5.ToString("F5")); s.Append(";");
            s.Append(LambdaIntegrator1.ToString("F5")); s.Append(";");
            s.Append(LambdaIntegrator2.ToString("F5")); s.Append(";");
            s.Append(LambdaHeatingAfterCats1.ToString("F2")); s.Append(";");
            s.Append(LambdaHeatingAfterCats2.ToString("F2")); s.Append(";");
            s.Append(LambdaHeatingBeforeCats1.ToString("F2")); s.Append(";");
            s.Append(LambdaHeatingBeforeCats2.ToString("F2")); s.Append(";");
            s.Append(VanosPositionExhaust.ToString("F3")); s.Append(";");
            s.Append(VanosPositionIntake.ToString("F3")); s.Append(";");
            s.Append(AtmosphericPressure); s.Append(";");
            s.Append(VoltageBattery.ToString("F2")); s.Append(";");
            s.Append(VoltageKL15.ToString("F2")); s.Append(";\n");
            return s.ToString();
        }

        public static string GenerateLogHeader()
        {
            var s = new StringBuilder();
            s.Append("Time;");
            s.Append("RPM;");
            s.Append("Speed;");
            s.Append("Throttle;");
            s.Append("Pedal;");
            s.Append("AFR;");
            s.Append("WideBandLambda;");
            s.Append("IntakePressure;");
            s.Append("AirMass;");
            s.Append("AirMassPerStroke;");
            s.Append("IgnitionAngle;");
            s.Append("InjectionTime;");
            s.Append("OilTemp;");
            s.Append("IntakeTemp;");
            s.Append("CoolantTemp;");
            s.Append("CoolantRadiatorTemp;");
            s.Append("ElectricFanSpeed;");
            s.Append("ISAPWM_IS;");
            s.Append("ISAPWM_ISA;");
            s.Append("KnockSensor2;");
            s.Append("KnockSensor5;");
            s.Append("LambdaIntegrator1;");
            s.Append("LambdaIntegrator2;");
            s.Append("LambdaHeatingAfterCats1;");
            s.Append("LambdaHeatingAfterCats2;");
            s.Append("LambdaHeatingBeforeCats1;");
            s.Append("LambdaHeatingBeforeCats2;");
            s.Append("VanosPositionExhaust;");
            s.Append("VanosPositionIntake;");
            s.Append("AtmosphericPressure;");
            s.Append("VoltageBattery;");
            s.Append("VoltageKL15;\n");
            return s.ToString();
        }
    }
}
