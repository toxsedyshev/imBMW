using imBMW.Diagnostics.DME;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Universal.App.Models;
using imBMW.Universal.App.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace imBMW.Universal.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DashboardPage : ExtendedPage
    {
        private static List<GaugeWatcher> gauges;

        public List<GaugeWatcher> Gauges
        {
            get
            {
                if (gauges == null)
                {
                    gauges = GaugeWatcher.FromSettingsList(Settings.Instance.Gauges);
                }
                return gauges;
            }
            private set
            {
                Set(ref gauges, value);
            }
        }

        public DashboardPage()
        {
            this.InitializeComponent();

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Gauges")
            {
                AreGaugesEnabled = false;
                Gauges = GaugeWatcher.FromSettingsList(Settings.Instance.Gauges);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Manager.AfterMessageReceived += Manager_AfterMessageReceived;
            InstrumentClusterElectronics.AverageSpeedChanged += InstrumentClusterElectronics_AverageSpeedChanged;
            InstrumentClusterElectronics.Consumption1Changed += InstrumentClusterElectronics_Consumption1Changed;
            InstrumentClusterElectronics.Consumption2Changed += InstrumentClusterElectronics_Consumption2Changed;
            InstrumentClusterElectronics.RangeChanged += InstrumentClusterElectronics_RangeChanged;
            InstrumentClusterElectronics.SpeedLimitChanged += InstrumentClusterElectronics_SpeedLimitChanged;
            InstrumentClusterElectronics.SpeedRPMChanged += InstrumentClusterElectronics_SpeedRPMChanged;
            InstrumentClusterElectronics.TemperatureChanged += InstrumentClusterElectronics_TemperatureChanged;

            AreGaugesEnabled = true;
            TestGauges();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Manager.AfterMessageReceived -= Manager_AfterMessageReceived;
            InstrumentClusterElectronics.AverageSpeedChanged -= InstrumentClusterElectronics_AverageSpeedChanged;
            InstrumentClusterElectronics.Consumption1Changed -= InstrumentClusterElectronics_Consumption1Changed;
            InstrumentClusterElectronics.Consumption2Changed -= InstrumentClusterElectronics_Consumption2Changed;
            InstrumentClusterElectronics.RangeChanged -= InstrumentClusterElectronics_RangeChanged;
            InstrumentClusterElectronics.SpeedLimitChanged -= InstrumentClusterElectronics_SpeedLimitChanged;
            InstrumentClusterElectronics.SpeedRPMChanged -= InstrumentClusterElectronics_SpeedRPMChanged;
            InstrumentClusterElectronics.TemperatureChanged -= InstrumentClusterElectronics_TemperatureChanged;
        }

        #region Gauges test

        DispatcherTimer testTimer;
        double testTimerTicks = 0;
        static bool wasTested;

        private void TestTimer_Tick(object sender, object e)
        {
            if (testTimerTicks > 200)
            {
                testTimer.Stop();
                testTimer = null;
                AreGaugesEnabled = true;
                return;
            }
            foreach (var g in Gauges)
            {
                TestGauge(g, testTimerTicks);
            }
            testTimerTicks += 8;
        }

        void TestGauge(GaugeWatcher g, double percent)
        {
            if (percent > 100)
            {
                percent = 200 - percent;
            }
            if (percent < 3)
            {
                percent = 0;
            }
            g.Percentage = percent;
            if (g.SecondaryWatcher != null)
            {
                g.SecondaryWatcher.Percentage = percent;
            }
        }

        private void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            if (e.CurrentIgnitionState == IgnitionState.Ign && e.PreviousIgnitionState != IgnitionState.Ign)
            {
                wasTested = false;
                if (IsNavigated)
                {
                    TestGauges();
                }
            }
        }

        void TestGauges()
        {
            //var av = new MS43JMGAnalogValues();
            //av.OilTemp = 95.5;
            //av.VoltageBattery = 14.1;
            //av.CoolantTemp = 93.1;
            //av.CoolantRadiatorTemp = 90.3;
            //Gauges.ForEach(g => g.Update(av));

            if (wasTested || InstrumentClusterElectronics.CurrentRPM > 0)
            {
                return;
            }

            AreGaugesEnabled = false;

            testTimer = new DispatcherTimer();
            testTimer.Interval = TimeSpan.FromMilliseconds(3);
            testTimer.Tick += TestTimer_Tick;
            testTimer.Start();

            //foreach (var g in Gauges)
            //{
            //    g.Init();
            //}

            wasTested = true;
        }

        #endregion

        bool AreGaugesEnabled
        {
            set
            {
                Gauges.ForEach(g => g.IsEnabled = value);
            }
        }

        private void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            if (MS43AnalogValues.CanParse(e.Message))
            {
                var av = new MS43JMGAnalogValues();
                av.Parse(e.Message);
                Gauges.ForEach(g => g.Update(av));
            }
        }
        
        private void InstrumentClusterElectronics_TemperatureChanged(TemperatureEventArgs e)
        {
            UpdateIKEGauge(GaugeType.CoolantTemperatureBC, e.Coolant);
            UpdateIKEGauge(GaugeType.OutsideTemperature, e.Outside);
        }

        private void InstrumentClusterElectronics_SpeedRPMChanged(SpeedRPMEventArgs e)
        {
            UpdateIKEGauge(GaugeType.SpeedBC, e.Speed);
            UpdateIKEGauge(GaugeType.RPMBC, e.RPM);
        }

        private void InstrumentClusterElectronics_SpeedLimitChanged(SpeedLimitEventArgs e)
        {
            UpdateIKEGauge(GaugeType.SpeedLimit, e.Value);
        }

        private void InstrumentClusterElectronics_RangeChanged(RangeEventArgs e)
        {
            UpdateIKEGauge(GaugeType.Range, e.Value);
        }

        private void InstrumentClusterElectronics_Consumption2Changed(ConsumptionEventArgs e)
        {
            UpdateIKEGauge(GaugeType.Consumption2, e.Value);
        }

        private void InstrumentClusterElectronics_Consumption1Changed(ConsumptionEventArgs e)
        {
            UpdateIKEGauge(GaugeType.Consumption1, e.Value);
        }

        private void InstrumentClusterElectronics_AverageSpeedChanged(AverageSpeedEventArgs e)
        {
            UpdateIKEGauge(GaugeType.AverageSpeed, e.Value);
        }
    }
}
