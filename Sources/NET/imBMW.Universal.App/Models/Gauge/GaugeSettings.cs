using imBMW.Diagnostics.DME;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Universal.App.Models
{
    public delegate double GaugeGetDMEValueDelegate(DMEAnalogValues av);

    public delegate void GaugeValueUpdatedDelegate(double value);

    public delegate void GaugeSubcribeToUpdatesDelegate(GaugeValueUpdatedDelegate callback);

    public class GaugeSettings : ObservableObject
    {
        private GaugeSettings secondaryGauge;

        private string name;

        public GaugeSettings(GaugeType type)
        {
            GaugeType = type;
        }

        public string Name
        {
            get
            {
                if (name != null)
                {
                    return name;
                }
                return GaugeType.ToString();
            }
            set
            {
                name = value;
            }
        }

        public string Dimension { get; set; }

        public GaugeGetDMEValueDelegate GetDMEValue { get; set; }

        public GaugeSubcribeToUpdatesDelegate SubcribeToUpdates { get; set; }

        public GaugeType GaugeType { get; set; } = GaugeType.Custom;
        
        public double MinValue { get; set; } = 0;

        public double MaxValue { get; set; } = 100;

        public double ZeroValue { get; set; } = 0;

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
}
