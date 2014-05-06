using System;
using Microsoft.SPOT;
using System.Collections;
using imBMW.Features.Menu.Screens;
using imBMW.Tools;

namespace imBMW.Features.Menu
{
    public class MenuBase
    {
        bool isEnabled;
        MenuScreen homeScreen;
        MenuScreen currentScreen;
        Stack navigationStack = new Stack();

        public MenuBase()
        {
            homeScreen = HomeScreen.Instance;
            CurrentScreen = homeScreen;
        }

        protected virtual void DrawScreen() { }

        protected virtual void ScreenSuspend()
        {
            ScreenNavigatedFrom(CurrentScreen);
        }

        protected virtual void ScreenWakeup()
        {
            ScreenNavigatedTo(CurrentScreen);
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value)
                {
                    return;
                }
                isEnabled = value;
                if (value)
                {
                    ScreenWakeup();
                    UpdateScreen();
                }
                else
                {
                    ScreenSuspend();
                }
            }
        }

        public virtual void UpdateScreen()
        {
            if (!IsEnabled)
            {
                return;
            }
            DrawScreen();
        }

        public void Navigate(MenuScreen screen)
        {
            if (screen == null)
            {
                Logger.Error("Navigation to null screen");
                return;
            }
            if (CurrentScreen == screen)
            {
                return;
            }
            navigationStack.Push(CurrentScreen);
            CurrentScreen = screen;
        }

        public void NavigateBack()
        {
            if (navigationStack.Count > 0)
            {
                CurrentScreen = navigationStack.Pop() as MenuScreen;
            }
            else
            {
                NavigateHome();
            }
        }

        public void NavigateHome()
        {
            CurrentScreen = homeScreen;
            navigationStack.Clear();
        }

        public void NavigateAfterHome(MenuScreen screen)
        {
            navigationStack.Clear();
            navigationStack.Push(homeScreen);
            CurrentScreen = screen;
        }

        public MenuScreen CurrentScreen
        {
            get
            {
                return currentScreen;
            }
            set
            {
                if (currentScreen == value || value == null)
                {
                    return;
                }
                ScreenNavigatedFrom(currentScreen);
                currentScreen = value;
                ScreenNavigatedTo(currentScreen);
                UpdateScreen();
            }
        }

        void ScreenNavigatedTo(MenuScreen screen)
        {
            if (screen == null || !screen.OnNavigatedTo(this))
            {
                return;
            }

            screen.ItemClicked += currentScreen_ItemClicked;
            screen.Updated += currentScreen_Updated;
        }

        void ScreenNavigatedFrom(MenuScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            screen.OnNavigatedFrom(this);

            screen.ItemClicked -= currentScreen_ItemClicked;
            screen.Updated -= currentScreen_Updated;
        }

        void currentScreen_Updated(MenuScreen screen)
        {
            UpdateScreen();
        }

        void currentScreen_ItemClicked(MenuScreen screen, MenuItem item)
        {
            switch (item.Action)
            {
                case MenuItemAction.GoToScreen:
                    Navigate(item.GoToScreen);
                    break;
                case MenuItemAction.GoBackScreen:
                    NavigateBack();
                    break;
                case MenuItemAction.GoHomeScreen:
                    NavigateHome();
                    break;
            }
        }
    }
}
