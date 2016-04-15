using System;
using Microsoft.SPOT;
using imBMW.Features.Localizations;
using imBMW.iBus.Devices.Real;

namespace imBMW.Features.Menu.Screens
{
    public class SpeedLimitScreen : MenuScreen
    {
        protected static SpeedLimitScreen instance;

        protected SpeedLimitScreen()
        {
            TitleCallback = s => Localization.Current.Limit;
            UpdateLimit();
            SetItems();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                InstrumentClusterElectronics.SpeedLimitChanged += InstrumentClusterElectronics_SpeedLimitChanged;
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                InstrumentClusterElectronics.SpeedLimitChanged -= InstrumentClusterElectronics_SpeedLimitChanged;
                return true;
            }
            return false;
        }

        private void InstrumentClusterElectronics_SpeedLimitChanged(SpeedLimitEventArgs e)
        {
            UpdateLimit();
        }

        void UpdateLimit()
        {
            Status = InstrumentClusterElectronics.SpeedLimit > 0 ? "" : InstrumentClusterElectronics.SpeedLimit + Localization.Current.KMH;
        }

        protected virtual void SetItems()
        {
            ClearItems();
            AddItem(new MenuItem(i => Localization.Current.LimitIncrease, i => InstrumentClusterElectronics.IncreaseSpeedLimit()));
            AddItem(new MenuItem(i => Localization.Current.LimitDecrease, i => InstrumentClusterElectronics.DecreaseSpeedLimit()));
            AddItem(new MenuItem(i => Localization.Current.LimitCurrentSpeed, i => InstrumentClusterElectronics.SetSpeedLimitToCurrentSpeed()));
            AddItem(new MenuItem(i => Localization.Current.TurnOff, i => InstrumentClusterElectronics.SetSpeedLimitOff()));

            this.AddBackButton();
        }

        public static SpeedLimitScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SpeedLimitScreen();
                }
                return instance;
            }
        }
    }
}
