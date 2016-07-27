using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Universal.App.Models
{
    public class GaugeSettings
    {
        public GaugeSettings AdditionalGauge { get; set; }

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
    }
}
