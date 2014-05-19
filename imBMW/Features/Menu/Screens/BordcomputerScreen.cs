using System;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.Features.Localizations;

namespace imBMW.Features.Menu.Screens
{
    public class BordcomputerScreen : MenuScreen
    {
        protected static BordcomputerScreen _instance;

        protected MenuItem _itemPlayer;
        protected MenuItem _itemFav;
        protected MenuItem _itemBc;
        protected MenuItem _itemSettings;

        protected DateTime _lastUpdated;

        protected BordcomputerScreen()
        {
            TitleCallback = s => Localization.Current.BordcomputerShort;
            SetItems();

            // TODO subscribe and unsubscribe ZKE and IKE and update voltage on navigation events
            BodyModule.BatteryVoltageChanged += v => { WithUpdateSuspended(s => Status = ""); if (!UpdateItems()) { OnUpdated(); } };
            InstrumentClusterElectronics.SpeedRPMChanged += e => UpdateItems();
            InstrumentClusterElectronics.TemperatureChanged += e => UpdateItems();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            var nav = base.OnNavigatedTo(menu);
            if (nav)
            {
                UpdateVoltage();
            }
            return nav;
        }

        protected bool UpdateItems()
        {
            //BodyModule.UpdateBatteryVoltage(); // TODO solve mem leak
            var now = DateTime.Now;
            if (_lastUpdated != DateTime.MinValue && (now - _lastUpdated).GetTotalSeconds() < 4)
            {
                return false;
            }
            _lastUpdated = now;
            OnUpdated();
            return true;
        }

        protected virtual void SetItems()
        {
            ClearItems();
            AddItem(new MenuItem(i => (Localization.Current.Speed + ":").AppendToLength(12) + InstrumentClusterElectronics.CurrentSpeed + Localization.Current.KMH));
            AddItem(new MenuItem(i => (Localization.Current.Revs + ":").AppendToLength(12) + InstrumentClusterElectronics.CurrentRPM));
            AddItem(new MenuItem(i => (Localization.Current.Voltage + ":").AppendToLength(12) + BodyModule.BatteryVoltage.ToString("F1") + " " + Localization.Current.VoltageShort, i => UpdateVoltage()));
            AddItem(new MenuItem(i =>
            {
                var coolant = InstrumentClusterElectronics.TemperatureCoolant == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureCoolant.ToString();
                return (Localization.Current.Engine + ":").AppendToLength(12) + coolant + "°C";
            }));
            AddItem(new MenuItem(i =>
            {
                var outside = InstrumentClusterElectronics.TemperatureOutside == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureOutside.ToString();
                return (Localization.Current.Outside + ":").AppendToLength(12) + outside + "°C";
            }));
            this.AddBackButton();
        }

        protected void UpdateVoltage()
        {
            Status = Localization.Current.Refreshing; 
            BodyModule.UpdateBatteryVoltage();
        }

        public static BordcomputerScreen Instance
        {
            get { return _instance ?? (_instance = new BordcomputerScreen()); }
        }
    }
}
