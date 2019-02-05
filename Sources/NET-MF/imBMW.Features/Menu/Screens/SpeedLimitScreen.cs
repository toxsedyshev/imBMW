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
            var navigatedTo = base.OnNavigatedTo(menu);
            if (navigatedTo && !IsNavigatedOnMultipleMenus)
            {
                InstrumentClusterElectronics.SpeedLimitChanged += InstrumentClusterElectronics_SpeedLimitChanged;
                UpdateLimit();
            }
            return navigatedTo;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            var navigatedFrom = base.OnNavigatedFrom(menu);
            if (navigatedFrom && !IsNavigated)
            {
                InstrumentClusterElectronics.SpeedLimitChanged -= InstrumentClusterElectronics_SpeedLimitChanged;
            }
            return navigatedFrom;
        }

        private void InstrumentClusterElectronics_SpeedLimitChanged(SpeedLimitEventArgs e)
        {
            UpdateLimit();
        }

        void UpdateLimit()
        {
            Status = InstrumentClusterElectronics.SpeedLimit == 0 ? "" : InstrumentClusterElectronics.SpeedLimit + Localization.Current.KMH;
        }

        protected virtual void SetItems()
        {
            ClearItems();
            AddItem(new MenuItem(i => Localization.Current.LimitIncrease, i => InstrumentClusterElectronics.IncreaseSpeedLimit()));
            AddItem(new MenuItem(i => Localization.Current.LimitDecrease, i => InstrumentClusterElectronics.DecreaseSpeedLimit()));
            AddItem(new MenuItem(i => Localization.Current.LimitCurrentSpeed, i => InstrumentClusterElectronics.SetSpeedLimitToCurrentSpeed()));
            AddItem(new MenuItem(i => InstrumentClusterElectronics.SpeedLimit == 0 ? Localization.Current.TurnOn : Localization.Current.TurnOff, i =>
            {
                if (InstrumentClusterElectronics.SpeedLimit == 0)
                {
                    InstrumentClusterElectronics.SetSpeedLimitOn();
                }
                else
                {
                    InstrumentClusterElectronics.SetSpeedLimitOff();
                }
            }));

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
