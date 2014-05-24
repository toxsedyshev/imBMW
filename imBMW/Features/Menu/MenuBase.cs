using System.Collections;
using imBMW.Features.Menu.Screens;
using imBMW.Tools;
using imBMW.iBus.Devices.Emulators;
using imBMW.Multimedia;
using System.Threading;

namespace imBMW.Features.Menu
{
    public abstract class MenuBase
    {
        bool _isEnabled;

        readonly MenuScreen _homeScreen;
        MenuScreen _currentScreen;
        readonly Stack _navigationStack = new Stack();

        protected MediaEmulator _mediaEmulator;

        protected MenuBase(MediaEmulator mediaEmulator)
        {
           _homeScreen = HomeScreen.Instance;
            CurrentScreen = _homeScreen;

            _mediaEmulator = mediaEmulator;
            mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            mediaEmulator.PlayerIsPlayingChanged += ShowPlayerStatus;
            mediaEmulator.PlayerStatusChanged += ShowPlayerStatus;
            mediaEmulator.PlayerChanged += mediaEmulator_PlayerChanged;
            mediaEmulator_PlayerChanged(mediaEmulator.Player);
        }

        #region MediaEmulator members

        Timer displayStatusDelayTimer;
        protected const int displayStatusDelay = 2000; // TODO make abstract

        protected abstract int StatusTextMaxlen { get; }

        protected abstract void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent);

        protected abstract void ShowPlayerStatus(IAudioPlayer player, bool isPlaying);

        protected void ShowPlayerStatus(IAudioPlayer player)
        {
            ShowPlayerStatus(player, player.IsPlaying);
        }

        protected void ShowPlayerStatus(IAudioPlayer player, string status)
        {
            if (!IsEnabled)
            {
                return;
            }
            if (displayStatusDelayTimer != null)
            {
                displayStatusDelayTimer.Dispose();
                displayStatusDelayTimer = null;
            }

            player.Menu.Status = status;
        }

        protected void ShowPlayerStatusWithDelay(IAudioPlayer player)
        {
            if (displayStatusDelayTimer != null)
            {
                displayStatusDelayTimer.Dispose();
                displayStatusDelayTimer = null;
            }

            displayStatusDelayTimer = new Timer(delegate
            {
                ShowPlayerStatus(player);
            }, null, displayStatusDelay, 0);
        }

        protected string TextWithIcon(string icon, string text = null)
        {
            if (StringHelpers.IsNullOrEmpty(text))
            {
                return icon;
            }
            if (icon.Length + text.Length < StatusTextMaxlen)
            {
                return icon + " " + text;
            }
            return icon + text;
        }

        void mediaEmulator_PlayerChanged(IAudioPlayer player)
        {
            HomeScreen.Instance.PlayerScreen = player.Menu;
        }

        void mediaEmulator_IsEnabledChanged(MediaEmulator emulator, bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        #endregion

        #region Drawing members

        protected virtual void DrawScreen(MenuScreenUpdateEventArgs args) { }

        protected virtual void ScreenSuspend()
        {
            ScreenNavigatedFrom(CurrentScreen);
        }

        protected virtual void ScreenWakeup()
        {
            ScreenNavigatedTo(CurrentScreen);
        }

        public virtual void UpdateScreen(MenuScreenUpdateReason reason, object item = null)
        {
            UpdateScreen(new MenuScreenUpdateEventArgs(reason, item));
        }

        public virtual void UpdateScreen(MenuScreenUpdateEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }
            DrawScreen(args);
        }

        void currentScreen_Updated(MenuScreen screen, MenuScreenUpdateEventArgs args)
        {
            UpdateScreen(args);
        }

        #endregion

        #region Navigation members

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
                    UpdateScreen(MenuScreenUpdateReason.Navigation);
                }
                else
                {
                    ScreenSuspend();
                }
            }
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
                CurrentScreen = (MenuScreen)_navigationStack.Pop();
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
                UpdateScreen(MenuScreenUpdateReason.Navigation);
            }
        }

        protected virtual void ScreenNavigatedTo(MenuScreen screen)
        {
            if (screen == null || !screen.OnNavigatedTo(this))
            {
                return;
            }

            screen.ItemClicked += currentScreen_ItemClicked;
            screen.Updated += currentScreen_Updated;
        }

        protected virtual void ScreenNavigatedFrom(MenuScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            screen.OnNavigatedFrom(this);

            screen.ItemClicked -= currentScreen_ItemClicked;
            screen.Updated -= currentScreen_Updated;
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

        #endregion
    }
}
