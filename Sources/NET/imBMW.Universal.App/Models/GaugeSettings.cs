using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Universal.App.Models
{
    public class GaugeSettings : ObservableObject
    {
        private GaugeSettings secondaryGauge;

        public string Name { get; set; }

        public string Dimention { get; set; }

        public GaugeField FieldType { get; set; } = GaugeField.Custom;

        public string Field { get; set; }

        public double MinValue { get; set; } = 0;

        public double MaxValue { get; set; } = 100;

        public double MinRed { get; set; } = double.MinValue;
        
        public double MinYellow { get; set; } = double.MinValue;

        public double MaxYellow { get; set; } = double.MaxValue;

        public double MaxRed { get; set; } = double.MaxValue;

        public string Format { get; set; }

        public string Suffix { get; set; }

        public double AddToValue { get; set; } = 0;

        public double MultiplyValue { get; set; } = 1;

        public GaugeSettings SecondaryGauge
        {
            get
            {
                return secondaryGauge;
            }

            set
            {
                Set(ref secondaryGauge, value);
            }
        }
    }

    public enum GaugeField : int
    {
        Custom,
        Consumption1,
        Consumption2,
        SpeedLimit,
        AverageSpeed,
        Range,
        ArrivalDistance,
        ArrivalTime,
        CoolantTemperature,
        OutsideTemperature,
        Voltage,
        Speed,
        RPM
    }
}
