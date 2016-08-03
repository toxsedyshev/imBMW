using imBMW.Diagnostics.DME;
using imBMW.iBus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Universal.App.Models
{
    public class GaugeWatcher : ObservableObject
    {
        object rawValue;
        string stringValue;
        double numValue;
        GaugeWatcher secondaryWatcher;

        public GaugeSettings Settings { get; protected set; }

        public object RawValue
        {
            get
            {
                return rawValue;
            }

            protected set
            {
                if (Set(ref rawValue, value))
                {
                    StringValue = string.Format("{0:" + Settings.Format + "}", value);
                    try
                    {
                        NumValue = (double)value;
                    }
                    catch
                    {
                        NumValue = 0;
                    }
                }
            }
        }

        public double Percentage
        {
            get
            {
                if (Settings.MaxValue - Settings.MinValue == 0)
                {
                    return 0;
                }
                var res = (NumValue - Settings.MinValue) / (Settings.MaxValue - Settings.MinValue);
                res = Math.Max(Math.Min(res, 100), 0);
                return res * 100;
            }
        }

        public double Angle
        {
            get
            {
                return 3.6 * Percentage;
            }
        }

        public double GrayAngleStart
        {
            get
            {
                return Angle + 1;
            }
        }

        public double GrayAngle
        {
            get
            {
                return 360 - GrayAngleStart - 1;
            }
        }

        public double NumValue
        {
            get
            {
                return numValue;
            }

            set
            {
                if (Set(ref numValue, value))
                {
                    OnPropertyChanged("Percentage");
                    OnPropertyChanged("Angle");
                }
            }
        }

        public string StringValue
        {
            get
            {
                return stringValue;
            }

            protected set
            {
                Set(ref stringValue, value);
            }
        }

        public GaugeWatcher SecondaryWatcher
        {
            get
            {
                return secondaryWatcher;
            }

            protected set
            {
                Set(ref secondaryWatcher, value);
            }
        }

        public GaugeWatcher(GaugeSettings settings)
        {
            Settings = settings;

            Settings.PropertyChanged += Settings_PropertyChanged;

            InitSecondaryWatcher();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SecondaryGauge")
            {
                InitSecondaryWatcher();
            }
        }

        private void InitSecondaryWatcher()
        {
            if (Settings.SecondaryGauge == null)
            {
                SecondaryWatcher = null;
            }
            else
            {
                SecondaryWatcher = new GaugeWatcher(Settings.SecondaryGauge);
            }
        }

        public void Update(DMEAnalogValues av)
        {
            try
            {
                RawValue = av.GetType().GetProperty(Settings.Field).GetValue(av);
            }
            catch
            {
                StringValue = "N/A";
            }

            SecondaryWatcher?.Update(av);
        }

        public static List<GaugeWatcher> FromSettingsList(IEnumerable<GaugeSettings> settingsList)
        {
            var list = new List<GaugeWatcher>();
            foreach (var s in settingsList)
            {
                list.Add(new GaugeWatcher(s));
            }
            return list;
        }
    }
}
