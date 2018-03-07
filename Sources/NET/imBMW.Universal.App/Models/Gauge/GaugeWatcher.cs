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
        bool isSubscribed;
        bool isEnabled = true;
        GaugeWatcher secondaryWatcher;
        GaugeSettings settings;

        static Brush redBrush = new SolidColorBrush(Colors.Red);
        static Brush yellowBrush = new SolidColorBrush(Colors.Yellow);
        static Brush greenBrush = new SolidColorBrush(Colors.Green);

        public GaugeWatcher(GaugeSettings settings)
        {
            Settings = settings;

            Settings.PropertyChanged += Settings_PropertyChanged;

            InitSecondaryWatcher();
        }

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

        void FormatValue(double value)
        {
            StringValue = value.ToString(Settings.Format) + Settings.Suffix;
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

        public bool IsEnabled { get => isEnabled; set => Set(ref isEnabled, value); }

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

        //public void Update(GaugeType field, double rawValue)
        //{
        //    SecondaryWatcher?.Update(field, rawValue);
        //    
        //    if (Settings.GaugeType == field)
        //    {
        //        RawValue = rawValue;
        //    }
        //}

        public void Update(DMEAnalogValues av)
        {
            SecondaryWatcher?.Update(av);

            if (!IsEnabled || Settings.GetDMEValue == null)
            {
                return;
            }
            try
            {
                RawValue = Settings.GetDMEValue(av);
            }
            catch
            {
                StringValue = "N/A";
            }
        }

        public void SubscribeToUpdates()
        {
            if (!isSubscribed)
            {
                isSubscribed = true;
                Settings.SubcribeToUpdates?.Invoke(value =>
                {
                    if (IsEnabled) RawValue = value;
                });
            }

            SecondaryWatcher.SubscribeToUpdates();
        }

        public void Init()
        {
            SecondaryWatcher?.Init();

            Percentage = 0;
        }
    }
}
