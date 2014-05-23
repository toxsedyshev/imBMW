using System;
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
            BodyModule.BatteryVoltageChanged += v => { WithUpdateSuspended(s => Status = ""); if (!UpdateItems()) { OnUpdated(MenuScreenUpdateReason.Refresh); } };
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
            if (lastUpdated != DateTime.MinValue && (now - lastUpdated).GetTotalSeconds() < 4)
            {
                return false;
            }
            lastUpdated = now;
            OnUpdated(MenuScreenUpdateReason.Refresh);
            return true;
        }

        protected virtual uint FirstColumnLength
        {
            get
            {
                var l = Math.Max(Localization.Current.Speed.Length, Localization.Current.Revs.Length);
                l = Math.Max(l, Localization.Current.Voltage.Length);
                l = Math.Max(l, Localization.Current.Engine.Length);
                l = Math.Max(l, Localization.Current.Outside.Length);
                return (uint)(l + 3);
            }
        }

        protected virtual void SetItems()
        {
            ClearItems();
            AddItem(new MenuItem(i => (Localization.Current.Speed + ":").AppendToLength(FirstColumnLength) + InstrumentClusterElectronics.CurrentSpeed + Localization.Current.KMH));
            AddItem(new MenuItem(i => (Localization.Current.Revs + ":").AppendToLength(FirstColumnLength) + InstrumentClusterElectronics.CurrentRPM));
            AddItem(new MenuItem(i => (Localization.Current.Voltage + ":").AppendToLength(FirstColumnLength) + BodyModule.BatteryVoltage.ToString("F1") + " " + Localization.Current.VoltageShort, i => UpdateVoltage()));
            AddItem(new MenuItem(i =>
            {
                var coolant = InstrumentClusterElectronics.TemperatureCoolant == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureCoolant.ToString();
                return (Localization.Current.Engine + ":").AppendToLength(FirstColumnLength) + coolant + "°C";
            }));
            AddItem(new MenuItem(i =>
            {
                var outside = InstrumentClusterElectronics.TemperatureOutside == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureOutside.ToString();
                return (Localization.Current.Outside + ":").AppendToLength(FirstColumnLength) + outside + "°C";
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
