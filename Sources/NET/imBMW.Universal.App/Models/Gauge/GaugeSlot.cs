using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Universal.App.Models
{
    public class GaugeSlot
    {
        public GaugeType PrimaryGauge { get; private set; }
        public GaugeType? SecondaryGauge { get; private set; }

        public GaugeSlot(GaugeType primaryGauge, GaugeType? secondaryGauge = null)
        {
            PrimaryGauge = primaryGauge;
            SecondaryGauge = secondaryGauge;
        }
    }
}
