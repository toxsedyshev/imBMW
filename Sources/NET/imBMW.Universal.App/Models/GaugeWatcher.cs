using imBMW.Diagnostics.DME;
using imBMW.iBus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace imBMW.Universal.App.Models
{
    public class GaugeWatcher : ObservableObject
    {
        double rawValue;
        string stringValue;
        double numValue;
        GaugeWatcher secondaryWatcher;
        GaugeSettings settings;

        static Brush redBrush = new SolidColorBrush(Colors.Red);
        static Brush yellowBrush = new SolidColorBrush(Colors.Yellow);
        static Brush greenBrush = new SolidColorBrush(Colors.Green);
        static Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>(); 

        public double RawValue
        {
            get
            {
                return rawValue;
            }

            set
            {
                if (Set(ref rawValue, value))
                {
                    NumValue = (value + Settings.AddToValue) * Settings.MultiplyValue;
                }
            }
        }

        void FormatValue(object value)
        {
            StringValue = string.Format("{0:" + Settings.Format + "}{1}", value, Settings.Suffix);
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
                res = Math.Max(Math.Min(res, 1), 0);
                return res * 100;
            }
            set
            {
                value = Math.Min(100, Math.Max(0, value));
                var val = (Settings.MinValue + (Settings.MaxValue - Settings.MinValue) * value / 100) / Settings.MultiplyValue - Settings.AddToValue;
                RawValue = val;
            }
        }

        public double Angle
        {
            get
            {
                return Math.Min(3.6 * Percentage, 359);
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
                return Math.Max(360 - GrayAngleStart - 1, 0);
            }
        }

        public Brush Foreground
        {
            get
            {
                if (NumValue < Settings.MinRed || NumValue > Settings.MaxRed)
                {
                    return redBrush;
                }
                if (NumValue < Settings.MinYellow || NumValue > Settings.MaxYellow)
                {
                    return yellowBrush;
                }
                return greenBrush;
            }
        }

        public double NumValue
        {
            get
            {
                return numValue;
            }

            protected set
            {
                if (Set(ref numValue, value))
                {
                    FormatValue(NumValue);
                    OnPropertyChanged("Percentage");
                    OnPropertyChanged("Angle");
                    OnPropertyChanged("GrayAngleStart");
                    OnPropertyChanged("GrayAngle");
                    OnPropertyChanged("Foreground");
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

        public GaugeSettings Settings
        {
            get
            {
                return settings;
            }

            protected set
            {
                Set(ref settings, value);
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
            SecondaryWatcher?.Update(av);

            if (Settings.FieldType != GaugeField.Custom)
            {
                return;
            }
            try
            {
                if (!properties.Keys.Contains(Settings.Field))
                {
                    properties.Add(Settings.Field, av.GetType().GetProperty(Settings.Field));
                }
                var obj = properties[Settings.Field].GetValue(av);
                if (obj is double)
                {
                    RawValue = (double)obj;
                }
                else if (obj is int)
                {
                    RawValue = Convert.ToDouble((int)obj);
                }
            }
            catch
            {
                StringValue = "N/A";
            }
        }

        public void Init()
        {
            SecondaryWatcher?.Init();

            Percentage = 0;
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
