using imBMW.Features.Localizations;

namespace imBMW.Features.Menu.Screens
{
    public class SettingsScreen : MenuScreen
    {
        protected static SettingsScreen _instance;

        private bool canChangeLanguage = true;

        protected SettingsScreen()
        {
            TitleCallback = s => Localization.Current.Settings;

            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();
            if (CanChangeLanguage)
            {
                AddItem(new MenuItem(i => Localization.Current.Language + ": " + Localization.Current.LanguageName, i => SwitchLanguage(), MenuItemType.Button, MenuItemAction.Refresh));
            }
            AddItem(new MenuItem(i => Localization.Current.ComfortWindows, i => Comfort.AutoCloseWindows = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoCloseWindows
            });
            AddItem(new MenuItem(i => Localization.Current.ComfortSunroof, i => Comfort.AutoCloseSunroof = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoCloseSunroof
            });
            AddItem(new MenuItem(i => Localization.Current.AutoLock, i => Comfort.AutoLockDoors = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoLockDoors
            });
            AddItem(new MenuItem(i => Localization.Current.AutoUnlock, i => Comfort.AutoUnlockDoors = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoUnlockDoors
            });
            this.AddBackButton();
        }

        public bool CanChangeLanguage
        {
            get { return canChangeLanguage; }
            set
            {
                if (canChangeLanguage == value)
                {
                    return;
                }
                canChangeLanguage = value;
                SetItems();
            }
        }

        void SwitchLanguage()
        {
            if (Localization.Current is EnglishLocalization)
            {
                Localization.Current = new RussianLocalization();
            }
            else
            {
                Localization.Current = new EnglishLocalization();
            }
        }

        public static SettingsScreen Instance
        {
            get { return _instance ?? (_instance = new SettingsScreen()); }
        }
    }
}
