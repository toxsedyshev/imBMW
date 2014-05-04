using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.Features.Localizations;

namespace imBMW.Features.Menu.Screens
{
    public class BordcomputerScreen : MenuScreen
    {
        protected static BordcomputerScreen instance;

        protected MenuItem itemPlayer;
        protected MenuItem itemFav;
        protected MenuItem itemBC;
        protected MenuItem itemSettings;

        protected DateTime lastUpdated;

        protected BordcomputerScreen()
        {
            TitleCallback = s => Localization.Current.BordcomputerShort;
            SetItems();

            // TODO subscribe and unsubscribe ZKE and IKE and update voltage on navigation events
            BodyModule.BatteryVoltageChanged += v => { WithUpdateSuspended(s => Status = ""); UpdateItems(); };
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

        protected void UpdateItems()
        {
            //BodyModule.UpdateBatteryVoltage(); // TODO solve mem leak
            var now = DateTime.Now;
            if (lastUpdated != DateTime.MinValue && (now - lastUpdated).GetTotalSeconds() < 4)
            {
                return;
            }
            lastUpdated = now;
            OnUpdated();
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
            get
            {
                if (instance == null)
                {
                    instance = new BordcomputerScreen();
                }
                return instance;
            }
        }
    }
}
