using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Universal.App.Models
{
    public class GaugeFactory
    {
        public static GaugeFactory Current { get; set; } = new GaugeFactory();

        public List<GaugeWatcher> CreateWatchers(IEnumerable<GaugeSlot> slotList)
        {
            var result = new List<GaugeWatcher>();
            foreach(var slot in slotList)
            {
                result.Add(CreateWatcher(slot));
            }
            return result;
        }

        public GaugeWatcher CreateWatcher(GaugeSlot slot)
        {
            return new GaugeWatcher(Create(slot));
        }

        public GaugeSettings Create(GaugeSlot slot)
        {
            var s = Create(slot.PrimaryGauge);
            if (slot.SecondaryGauge.HasValue)
            {
                s.SecondaryGauge = Create(slot.SecondaryGauge.Value);
            }
            return s;
        }

        public GaugeSettings Create(GaugeType type)
        {
            switch (type)
            {
                case GaugeType.RPM:
                    return new GaugeSettings(type)
                    {
                        Name = "RPM",
                        GetDMEValue = av => av.RPM,
                        Format = "N0",
                        MinValue = 0,
                        MaxValue = 8000,
                    };
                case GaugeType.Speed:
                    return new GaugeSettings(type)
                    {
                        Name = "Speed",
                        GetDMEValue = av => av.Speed,
                        Format = "N0",
                        Dimension = "km/h",
                        MinValue = 0,
                        MaxValue = 300,
                    };
                case GaugeType.Throttle:
                    return new GaugeSettings(type)
                    {
                        Name = "Throttle",
                        GetDMEValue = av => av.Throttle,
                        Format = "N0",
                        Suffix = "°",
                        MinValue = 0,
                        MaxValue = 90,
                    };
                case GaugeType.Pedal:
                    return new GaugeSettings(type)
                    {
                        Name = "Pedal",
                        GetDMEValue = av => av.Pedal,
                        Format = "N0",
                        Suffix = "°",
                        MinValue = 0,
                        MaxValue = 90,
                    };
                case GaugeType.AFR:
                    return new GaugeSettings(type)
                    {
                        Name = "AFR",
                        GetDMEValue = av => av.AFR,
                        Format = "F1",
                        Dimension = "Air/Fuel",
                        MinValue = 7.5,
                        MaxValue = 22.5,
                        MinRed = 10,
                        MinYellow = 11,
                        MaxYellow = 14.7,
                        MaxRed = 15.5
                    };
                case GaugeType.WideBandLambda:
                    return new GaugeSettings(type)
                    {
                        Name = "Lambda",
                        GetDMEValue = av => av.WideBandLambda,
                        Format = "F2",
                        MinValue = 0.5,
                        MaxValue = 1.5,
                    };
                case GaugeType.IntakePressure:
                    return new GaugeSettings(type)
                    {
                        Name = "Boost",
                        GetDMEValue = av => av.IntakePressure,
                        Format = "F2",
                        Dimension = "Bar",
                        MinValue = -1,
                        MaxValue = 2,
                        MinYellow = -0.01,
                        MaxYellow = 0.5,
                        MaxRed = 1.0,
                        AddToValue = -1000,
                        MultiplyValue = 0.001,
                        ZeroValue = 0.01
                    };
                case GaugeType.FuelPressure:
                    return new GaugeSettings(type)
                    {
                        Name = "Fuel",
                        GetDMEValue = av => av.FuelPressure,
                        Format = "F2",
                        Dimension = "Bar",
                        MinValue = 0,
                        MaxValue = 6,
                        MultiplyValue = 0.001,
                        ZeroValue = 0.12
                    };
                case GaugeType.OilPressure:
                    return new GaugeSettings(type)
                    {
                        Name = "Oil",
                        GetDMEValue = av => av.OilPressure,
                        Format = "F2",
                        Dimension = "Bar",
                        MinValue = 0,
                        MaxValue = 10,
                        MultiplyValue = 0.001,
                        ZeroValue = 0.1
                    };
                case GaugeType.IsMethanolFailsafe:
                    return new GaugeSettings(type)
                    {
                        Name = "Status",
                        Dimension = "Methanol",
                        Format = "OK/Fail",
                        GetDMEValue = av => av.IsMethanolFailsafe ? 1 : 0,
                        MinValue = 0,
                        MaxValue = 1,
                        MaxRed = 0.9
                    };
                case GaugeType.IsMethanolInjecting:
                    return new GaugeSettings(type)
                    {
                        Name = "Injection",
                        Dimension = "Methanol",
                        Format = "Idle/Active",
                        GetDMEValue = av => av.IsMethanolInjecting ? 1 : 0,
                        MinValue = 0,
                        MaxValue = 1
                    };
                case GaugeType.AirMass:
                    return new GaugeSettings(type)
                    {
                        Name = "Air Mass",
                        GetDMEValue = av => av.AirMass,
                        Format = "N0",
                        MinValue = 0,
                        MaxValue = 1500,
                        MinYellow = 500
                    };
                case GaugeType.AirMassPerStroke:
                    return new GaugeSettings(type)
                    {
                        Name = "Load",
                        GetDMEValue = av => av.AirMassPerStroke,
                        Format = "N0",
                        Dimension = "mg/stroke",
                        MinValue = 0,
                        MaxValue = 1500,
                    };
                case GaugeType.IgnitionAngle:
                    return new GaugeSettings(type)
                    {
                        Name = "Ignition",
                        GetDMEValue = av => av.IgnitionAngle,
                        Format = "F1",
                        Suffix = "°",
                        Dimension = "BTDC",
                        MinValue = -24,
                        MaxValue = 72,
                    };
                case GaugeType.InjectionTime:
                    return new GaugeSettings(type)
                    {
                        Name = "Injection",
                        GetDMEValue = av => av.InjectionTime,
                        Format = "F2",
                        Dimension = "ms",
                        MinValue = 0,
                        MaxValue = 100,
                    };
                case GaugeType.OilTemperature:
                    return new GaugeSettings(type)
                    {
                        Name = "Oil",
                        GetDMEValue = av => av.OilTemp,
                        Format = "N0",
                        Suffix = "°C",
                        Dimension = "Temperature",
                        MinValue = 0,
                        MaxValue = 150,
                        MinYellow = 75,
                        MaxYellow = 95,
                        MaxRed = 105
                    };
                case GaugeType.IntakeTemperature:
                    return new GaugeSettings(type)
                    {
                        Name = "IAT",
                        GetDMEValue = av => av.IntakeTemp,
                        Format = "N0",
                        Suffix = "°C",
                        Dimension = "Before Cooler",
                        MinValue = -30,
                        MaxValue = 100,
                        MaxYellow = 30,
                        MaxRed = 60,
                    };
                case GaugeType.IntakeTemperatureAfterCooler:
                    return new GaugeSettings(type)
                    {
                        Name = "IAT",
                        GetDMEValue = av => av.IntakeTempAfterCooler,
                        Format = "N0",
                        Suffix = "°C",
                        Dimension = "After Cooler",
                        MinValue = 0,
                        MaxValue = 100,
                        MaxYellow = 30,
                        MaxRed = 40
                    };
                case GaugeType.CoolerInTemperature:
                    return new GaugeSettings(type)
                    {
                        Name = "Inlet",
                        GetDMEValue = av => av.CoolerInTemp,
                        Format = "N0",
                        Suffix = "°C",
                        Dimension = "Liquid Intercooler",
                        MinValue = 0,
                        MaxValue = 100,
                        MaxYellow = 30,
                        MaxRed = 40
                    };
                case GaugeType.CoolerOutTemperature:
                    return new GaugeSettings(type)
                    {
                        Name = "Outlet",
                        GetDMEValue = av => av.CoolerOutTemp,
                        Format = "N0",
                        Suffix = "°C",
                        Dimension = "Liquid Intercooler",
                        MinValue = 0,
                        MaxValue = 100,
                        MaxYellow = 30,
                        MaxRed = 40
                    };
                case GaugeType.CoolantTemperature:
                    return new GaugeSettings(type)
                    {
                        Name = "Coolant",
                        GetDMEValue = av => av.CoolantTemp,
                        Format = "N0",
                        Suffix = "°C",
                        MinValue = 0,
                        MaxValue = 150,
                        MinYellow = 75,
                        MaxYellow = 95,
                        MaxRed = 105,
                    };
                case GaugeType.CoolantRadiatorTemperature:
                    return new GaugeSettings(type)
                    {
                        Name = "Radiator",
                        GetDMEValue = av => av.CoolantRadiatorTemp,
                        Format = "N0",
                        Suffix = "°C",
                        MinValue = 0,
                        MaxValue = 150,
                        MinYellow = 75,
                        MaxYellow = 95,
                        MaxRed = 105,
                    };
                case GaugeType.ElectricFanSpeed:
                    return new GaugeSettings(type)
                    {
                        Name = "Fan",
                        GetDMEValue = av => av.ElectricFanSpeed,
                        Format = "N0",
                        Suffix = "%",
                        MinValue = 0,
                        MaxValue = 100,
                        MaxYellow = 70,
                        MaxRed = 90
                    };
                case GaugeType.ISAPWM_IS:
                    return new GaugeSettings(type)
                    {
                        Name = "Idle PWM IS",
                        GetDMEValue = av => av.ISAPWM_IS,
                        Format = "N0",
                        Dimension = "",
                        MinValue = 0,
                        MaxValue = 100,
                    };
                case GaugeType.ISAPWM_ISA:
                    return new GaugeSettings(type)
                    {
                        Name = "Idle PWM ISA",
                        GetDMEValue = av => av.ISAPWM_ISA,
                        Format = "N0",
                        Dimension = "",
                        MinValue = 0,
                        MaxValue = 100,
                    };
                case GaugeType.KnockSensor2:
                    return new GaugeSettings(type)
                    {
                        Name = "Knock 2",
                        GetDMEValue = av => av.KnockSensor2,
                        Format = "F2",
                        Suffix = " V",
                        MinValue = 0,
                        MaxValue = 5,
                    };
                case GaugeType.KnockSensor5:
                    return new GaugeSettings(type)
                    {
                        Name = "Knock 5",
                        GetDMEValue = av => av.KnockSensor5,
                        Format = "F2",
                        Suffix = " V",
                        MinValue = 0,
                        MaxValue = 5,
                    };
                case GaugeType.LambdaIntegrator1:
                    return new GaugeSettings(type)
                    {
                        Name = "Lambda 1",
                        GetDMEValue = av => av.LambdaIntegrator1,
                        Format = "F2",
                        MinValue = 0.5,
                        MaxValue = 1.5,
                    };
                case GaugeType.LambdaIntegrator2:
                    return new GaugeSettings(type)
                    {
                        Name = "Lambda 2",
                        GetDMEValue = av => av.LambdaIntegrator2,
                        Format = "F2",
                        MinValue = 0.5,
                        MaxValue = 1.5,
                    };
                case GaugeType.LambdaHeatingAfterCats1:
                    return new GaugeSettings(type)
                    {
                        Name = "LHAC1",
                        GetDMEValue = av => av.LambdaHeatingAfterCats1,
                        Format = "F2",
                        Dimension = "",
                        MinValue = 0,
                        MaxValue = 100,
                    };
                case GaugeType.LambdaHeatingAfterCats2:
                    return new GaugeSettings(type)
                    {
                        Name = "LHAC2",
                        GetDMEValue = av => av.LambdaHeatingAfterCats2,
                        Format = "F2",
                        Dimension = "",
                        MinValue = 0,
                        MaxValue = 100,
                    };
                case GaugeType.LambdaHeatingBeforeCats1:
                    return new GaugeSettings(type)
                    {
                        Name = "LHBC1",
                        GetDMEValue = av => av.LambdaHeatingBeforeCats1,
                        Format = "F2",
                        Dimension = "",
                        MinValue = 0,
                        MaxValue = 100,
                    };
                case GaugeType.LambdaHeatingBeforeCats2:
                    return new GaugeSettings(type)
                    {
                        Name = "LHBC2",
                        GetDMEValue = av => av.LambdaHeatingBeforeCats2,
                        Format = "F2",
                        Dimension = "",
                        MinValue = 0,
                        MaxValue = 100,
                    };
                case GaugeType.VanosPositionExhaust:
                    return new GaugeSettings(type)
                    {
                        Name = "VanosPositionExhaust",
                        GetDMEValue = av => av.VanosPositionExhaust,
                        Format = "F2",
                        Suffix = "°",
                        MinValue = -160,
                        MaxValue = -60,
                    };
                case GaugeType.VanosPositionIntake:
                    return new GaugeSettings(type)
                    {
                        Name = "VanosPositionIntake",
                        GetDMEValue = av => av.VanosPositionIntake,
                        Format = "F2",
                        Suffix = "°",
                        MinValue = 60,
                        MaxValue = 160,
                    };
                case GaugeType.AtmosphericPressure:
                    return new GaugeSettings(type)
                    {
                        Name = "AtmosphericPressure",
                        GetDMEValue = av => av.AtmosphericPressure,
                        Format = "F2",
                        Dimension = "Bar",
                        MinValue = 0.9,
                        MaxValue = 1.1,
                        MultiplyValue = 0.001
                    };
                case GaugeType.VoltageBattery:
                    return new GaugeSettings(type)
                    {
                        Name = "Battery",
                        GetDMEValue = av => av.VoltageBattery,
                        Format = "F1",
                        Suffix = " V",
                        MinValue = 9,
                        MaxValue = 16,
                        MinRed = 13.3,
                        MinYellow = 13.6,
                        MaxYellow = 14.1,
                        MaxRed = 14.5
                    };
                case GaugeType.VoltageKL15:
                    return new GaugeSettings(type)
                    {
                        Name = "KL15",
                        GetDMEValue = av => av.VoltageKL15,
                        Format = "F1",
                        Suffix = " V",
                        MinValue = 9,
                        MaxValue = 16,
                        MinRed = 13.3,
                        MinYellow = 13.6,
                        MaxYellow = 14.1,
                        MaxRed = 14.5
                    };
                default:
                    //return new GaugeSettings(type);
                    throw new Exception("Not supported gauge type.");
            }
        }
    }
}
