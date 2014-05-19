using System.Collections;
using imBMW.Features.Menu.Screens;
using imBMW.Tools;

namespace imBMW.Features.Menu
{
    public class MenuBase
    {
        bool _isEnabled;

        readonly MenuScreen _homeScreen;
        MenuScreen _currentScreen;
        readonly Stack _navigationStack = new Stack();

        public MenuBase()
        {
            _homeScreen = HomeScreen.Instance;
            CurrentScreen = _homeScreen;
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
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value)
                {
                    return;
                }
                _isEnabled = value;
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
            _navigationStack.Push(CurrentScreen);
            CurrentScreen = screen;
        }

        public void NavigateBack()
        {
            if (_navigationStack.Count > 0)
            {
                CurrentScreen = _navigationStack.Pop() as MenuScreen;
            }
            else
            {
                NavigateHome();
            }
        }

        public void NavigateHome()
        {
            CurrentScreen = _homeScreen;
            _navigationStack.Clear();
        }

        public void NavigateAfterHome(MenuScreen screen)
        {
            _navigationStack.Clear();
            _navigationStack.Push(_homeScreen);
            CurrentScreen = screen;
        }

        public MenuScreen CurrentScreen
        {
            get
            {
                return _currentScreen;
            }
            set
            {
                if (_currentScreen == value || value == null)
                {
                    return;
                }
                ScreenNavigatedFrom(_currentScreen);
                _currentScreen = value;
                ScreenNavigatedTo(_currentScreen);
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
