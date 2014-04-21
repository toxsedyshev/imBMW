using System;
using Microsoft.SPOT;

namespace imBMW.Features.Menu.Screens
{
    public class HomeScreen : MenuScreen
    {
        protected static HomeScreen instance;

        protected MenuItem itemPlayer;
        protected MenuItem itemFav;
        protected MenuItem itemBC;
        protected MenuItem itemSettings;
        protected MenuItem itemPhone;

        protected HomeScreen()
        {
            Title = "imBMW";

            itemPlayer = new MenuItem("Плеер", MenuItemType.Button, MenuItemAction.GoToScreen);
            itemPhone = new MenuItem("Телефон", MenuItemType.Button, MenuItemAction.GoToScreen);
            itemFav = new MenuItem("Избранное", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = null // TODO fav screen
            };
            itemBC = new MenuItem("Борткомпьютер", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = BordcomputerScreen.Instance
            };
            itemSettings = new MenuItem("Настройки", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = null // TODO settings screen
            };
            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();
            if (itemPlayer.GoToScreen != null)
            {
                AddItem(itemPlayer);
            }
            if (itemPhone.GoToScreen != null)
            {
                AddItem(itemPhone);
            }
            AddItem(itemFav);
            AddItem(itemBC);
            AddItem(itemSettings);
        }

        public MenuScreen PlayerScreen
        {
            get
            {
                return itemPlayer.GoToScreen;
            }
            set
            {
                // TODO check it is shown now
                itemPlayer.GoToScreen = value;
                SetItems();
            }
        }

        public MenuScreen PhoneScreen
        {
            get
            {
                return itemPhone.GoToScreen;
            }
            set
            {
                itemPhone.GoToScreen = value;
                SetItems();
            }
        }

        public static HomeScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HomeScreen();
                }
                return instance;
            }
        }
    }
}
