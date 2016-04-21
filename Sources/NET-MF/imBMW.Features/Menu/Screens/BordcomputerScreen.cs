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
                InstrumentClusterElectronics.AverageSpeedChanged += InstrumentClusterElectronics_AverageSpeedChanged;
                InstrumentClusterElectronics.Consumption1Changed += InstrumentClusterElectronics_Consumption1Changed;
                InstrumentClusterElectronics.Consumption2Changed += InstrumentClusterElectronics_Consumption2Changed;
                InstrumentClusterElectronics.RangeChanged += InstrumentClusterElectronics_RangeChanged;
                InstrumentClusterElectronics.SpeedLimitChanged += InstrumentClusterElectronics_SpeedLimitChanged;

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
                InstrumentClusterElectronics.AverageSpeedChanged -= InstrumentClusterElectronics_AverageSpeedChanged;
                InstrumentClusterElectronics.Consumption1Changed -= InstrumentClusterElectronics_Consumption1Changed;
                InstrumentClusterElectronics.Consumption2Changed -= InstrumentClusterElectronics_Consumption2Changed;
                InstrumentClusterElectronics.RangeChanged -= InstrumentClusterElectronics_RangeChanged;
                InstrumentClusterElectronics.SpeedLimitChanged -= InstrumentClusterElectronics_SpeedLimitChanged;
                return true;
            }
            return false;
        }

        private void InstrumentClusterElectronics_SpeedLimitChanged(SpeedLimitEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_RangeChanged(RangeEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_Consumption2Changed(ConsumptionEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_Consumption1Changed(ConsumptionEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_AverageSpeedChanged(AverageSpeedEventArgs e)
        {
            UpdateItems();
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
                var l = System.Math.Max(Localization.Current.Speed.Length, Localization.Current.Revs.Length);
                l = System.Math.Max(l, Localization.Current.Voltage.Length);
                l = System.Math.Max(l, Localization.Current.Engine.Length);
                l = System.Math.Max(l, Localization.Current.Outside.Length);
                return (uint)(l + 3);
            }
        }

        protected virtual void SetItems()
        {
            ClearItems();
            AddItem(new MenuItem(i => Localization.Current.Speed + ": " + InstrumentClusterElectronics.CurrentSpeed + Localization.Current.KMH));
            AddItem(new MenuItem(i => Localization.Current.Revs + ": " + InstrumentClusterElectronics.CurrentRPM));
            AddItem(new MenuItem(i => Localization.Current.Consumption + " 1: " + InstrumentClusterElectronics.Consumption1.ToString("F1"), i => InstrumentClusterElectronics.ResetConsumption1()));
            AddItem(new MenuItem(i => Localization.Current.Consumption + " 2: " + InstrumentClusterElectronics.Consumption2.ToString("F1"), i => InstrumentClusterElectronics.ResetConsumption2()));
            AddItem(new MenuItem(i => Localization.Current.Range + ": " + InstrumentClusterElectronics.Range + Localization.Current.KM));

            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + (BodyModule.BatteryVoltage > 0 ? BodyModule.BatteryVoltage.ToString("F1") : "-") + " " + Localization.Current.VoltageShort, i => UpdateVoltage()));
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
            AddItem(new MenuItem(i => Localization.Current.Limit + ": " + (InstrumentClusterElectronics.SpeedLimit == 0 ? "-" : InstrumentClusterElectronics.SpeedLimit + Localization.Current.KMH), MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = SpeedLimitScreen.Instance
            });

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
