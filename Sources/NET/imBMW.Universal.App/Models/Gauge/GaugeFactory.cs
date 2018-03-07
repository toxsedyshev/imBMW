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
            var s = new GaugeSettings { GaugeType = type };
            switch (type)
            {
                
            }
            return s;
        }
    }
}
