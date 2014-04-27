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

        protected void ScreenSuspend()
        {
            ScreenUnnavigated(CurrentScreen);
        }

        protected void ScreenWakeup()
        {
            ScreenNavigated(CurrentScreen);
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
            navigationStack.Push(currentScreen);
            CurrentScreen = screen;
        }

        public void NavigateBack()
        {
            if (navigationStack.Count > 0)
            {
                CurrentScreen = navigationStack.Pop() as MenuScreen;
            }
        }

        public void NavigateHome()
        {
            CurrentScreen = homeScreen;
            navigationStack.Clear();
        }

        public MenuScreen CurrentScreen
        {
            get
            {
                return currentScreen;
            }
            set
            {
                if (currentScreen == value)
                {
                    return;
                }
                ScreenUnnavigated(currentScreen);
                currentScreen = value;
                ScreenNavigated(currentScreen);
                UpdateScreen();
            }
        }

        void ScreenNavigated(MenuScreen screen)
        {
            if (screen == null || !IsEnabled) // TODO check not navigated
            {
                return;
            }

            screen.ItemClicked += currentScreen_ItemClicked;
            screen.Updated += currentScreen_Updated;

            // TODO notify screen
        }

        void ScreenUnnavigated(MenuScreen screen)
        {
            if (screen == null) // TODO check navigated
            {
                return;
            }

            // TODO notify screen

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
