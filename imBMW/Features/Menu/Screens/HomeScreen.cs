using imBMW.Features.Localizations;

namespace imBMW.Features.Menu.Screens
{
    public class HomeScreen : MenuScreen
    {
        protected static HomeScreen _instance;

        protected MenuItem _itemPlayer;
        protected MenuItem _itemFav;
        protected MenuItem _itemBc;
        protected MenuItem _itemSettings;
        protected MenuItem _itemPhone;

        protected HomeScreen()
        {
            Title = "imBMW";

            _itemPlayer = new MenuItem(i => Localization.Current.Player, MenuItemType.Button, MenuItemAction.GoToScreen);
            _itemPhone = new MenuItem(i => Localization.Current.Phone, MenuItemType.Button, MenuItemAction.GoToScreen);
            _itemFav = new MenuItem(i => Localization.Current.QuickAccess, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = null // TODO fav screen
            };
            _itemBc = new MenuItem(i => Localization.Current.Bordcomputer, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = BordcomputerScreen.Instance
            };
            _itemSettings = new MenuItem(i => Localization.Current.Settings, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = SettingsScreen.Instance
            };
            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();
            if (_itemPlayer.GoToScreen != null)
            {
                AddItem(_itemPlayer);
            }
            if (_itemPhone.GoToScreen != null)
            {
                AddItem(_itemPhone);
            }
            //AddItem(itemFav);
            AddItem(_itemBc);
            AddItem(_itemSettings);
        }

        public MenuScreen PlayerScreen
        {
            get
            {
                return _itemPlayer.GoToScreen;
            }
            set
            {
                // TODO check it is shown now and renavigate
                _itemPlayer.GoToScreen = value;
                SetItems();
            }
        }

        public MenuScreen PhoneScreen
        {
            get
            {
                return _itemPhone.GoToScreen;
            }
            set
            {
                _itemPhone.GoToScreen = value;
                SetItems();
            }
        }

        public static HomeScreen Instance
        {
            get { return _instance ?? (_instance = new HomeScreen()); }
        }
    }
}
