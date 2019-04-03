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
                case GaugeType.IntakePressure:
                    return new GaugeSettings { Name = "Boost", GetDMEValue = av => av.IntakePressure, Format = "F2", Dimension = "Bar", MinValue = -1, MaxValue = 1, MinYellow = -0.01, MaxYellow = 0.5, MaxRed = 0.8, AddToValue = -1000, MultiplyValue = 0.001, GaugeType = type };
                default:
                    return new GaugeSettings { GaugeType = type };
                    //throw new Exception("Not supported gauge type.");
            }
        }
    }
}
