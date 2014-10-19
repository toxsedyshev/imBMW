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
        protected bool needUpdateVoltage;

        const int updateLimitSeconds = 3;

        protected BordcomputerScreen()
        {
            TitleCallback = s => Localization.Current.BordcomputerShort;
            SetItems();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                BodyModule.BatteryVoltageChanged += BodyModule_BatteryVoltageChanged;
                InstrumentClusterElectronics.SpeedRPMChanged += InstrumentClusterElectronics_SpeedRPMChanged;
                InstrumentClusterElectronics.TemperatureChanged += InstrumentClusterElectronics_TemperatureChanged;

                UpdateVoltage();
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                BodyModule.BatteryVoltageChanged -= BodyModule_BatteryVoltageChanged;
                InstrumentClusterElectronics.SpeedRPMChanged -= InstrumentClusterElectronics_SpeedRPMChanged;
                InstrumentClusterElectronics.TemperatureChanged -= InstrumentClusterElectronics_TemperatureChanged;
                return true;
            }
            return false;
        }

        void InstrumentClusterElectronics_TemperatureChanged(TemperatureEventArgs e)
        {
            UpdateItems();
        }

        void InstrumentClusterElectronics_SpeedRPMChanged(SpeedRPMEventArgs e)
        {
            UpdateItems();
        }

        void BodyModule_BatteryVoltageChanged(double voltage)
        {
            UpdateItems();
        }

        protected bool UpdateItems(bool force = false)
        {
            var now = DateTime.Now;
            int span;
            if (!force && lastUpdated != DateTime.MinValue && (span = (now - lastUpdated).GetTotalSeconds()) < updateLimitSeconds)
            {
                if (needUpdateVoltage) // span > updateLimitSeconds / 2 && 
                {
                    UpdateVoltage();
                }
                return false;
            }
            lastUpdated = now;
            OnUpdated(MenuScreenUpdateReason.Refresh);
            needUpdateVoltage = true;
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
            AddItem(new MenuItem(i => Localization.Current.Speed + ": " + InstrumentClusterElectronics.CurrentSpeed + Localization.Current.KMH));
            AddItem(new MenuItem(i => Localization.Current.Revs + ": " + InstrumentClusterElectronics.CurrentRPM));
            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + BodyModule.BatteryVoltage.ToString("F1") + " " + Localization.Current.VoltageShort, i => UpdateVoltage()));
            AddItem(new MenuItem(i =>
            {
                var coolant = InstrumentClusterElectronics.TemperatureCoolant == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureCoolant.ToString();
                return Localization.Current.Engine + ": " + coolant + Localization.Current.DegreeCelsius;
            }));
            AddItem(new MenuItem(i =>
            {
                var outside = InstrumentClusterElectronics.TemperatureOutside == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureOutside.ToString();
                return Localization.Current.Outside + ": " + outside + Localization.Current.DegreeCelsius;
            }));
            this.AddBackButton();
        }

        protected void UpdateVoltage()
        {
            needUpdateVoltage = false;
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
