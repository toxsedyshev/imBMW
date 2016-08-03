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

        public string Field { get; set; }

        public double MinValue { get; set; }

        public double MaxValue { get; set; }

        public double MinRed { get; set; }
        
        public double MinYellow { get; set; }

        public double MaxYellow { get; set; }

        public double MaxRed { get; set; }

        public string Format { get; set; }

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
