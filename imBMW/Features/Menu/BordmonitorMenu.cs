using System;
using imBMW.iBus.Devices.Real;
using imBMW.iBus;
using imBMW.Tools;
using System.Threading;
using imBMW.Features.Menu.Screens;
using imBMW.iBus.Devices.Emulators;
using imBMW.Multimedia;
using imBMW.Features.Localizations;

namespace imBMW.Features.Menu
{
    public class BordmonitorMenu : MenuBase
    {
        static BordmonitorMenu _instance;

        bool _skipRefreshScreen;
        bool _skipClearScreen;
        bool _skipClearTillRefresh;
        bool _disableRadioMenu;
        bool _isScreenSwitched;
        readonly object _drawLock = new object();

        private BordmonitorMenu(MediaEmulator mediaEmulator)
            : base(mediaEmulator)
        {
            mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
        }

        public static BordmonitorMenu Init(MediaEmulator mediaEmulator)
        {
            if (_instance != null)
            {
                // TODO implement hot switch of emulators
                throw new Exception("Already inited");
            }
            _instance = new BordmonitorMenu(mediaEmulator);
            return _instance;
        }

        #region Player items

        protected override int StatusTextMaxlen { get { return 11; } }

        protected override void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        {
            string s = isPlaying ? Localization.Current.Playing : Localization.Current.Paused;
            ShowPlayerStatus(player, s);
        }

        protected override void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent)
        {
            if (!IsEnabled)
            {
                return;
            }
            bool showAfterWithDelay = false;
            switch (playerEvent)
            {
                case PlayerEvent.Next:
                    status = Localization.Current.Next;
                    showAfterWithDelay = true;
                    break;
                case PlayerEvent.Prev:
                    status = Localization.Current.Previous;
                    showAfterWithDelay = true;
                    break;
                case PlayerEvent.Playing:
                    status = TextWithIcon(">", status);
                    break;
                case PlayerEvent.Current:
                    status = TextWithIcon("\x07", status);
                    break;
                case PlayerEvent.Voice:
                    status = TextWithIcon("*", status);
                    break;
            }
            ShowPlayerStatus(player, status);
            if (showAfterWithDelay)
            {
                ShowPlayerStatusWithDelay(player);
            }
        }

        void mediaEmulator_IsEnabledChanged(MediaEmulator emulator, bool isEnabled)
        {
            if (!isEnabled)
            {
                Bordmonitor.EnableRadioMenu();
            }
        }

        #endregion

        #region Screen items

        protected override void ScreenWakeup()
        {
            base.ScreenWakeup();

            _disableRadioMenu = true;
        }

        public override void UpdateScreen(MenuScreenUpdateEventArgs args)
        {
            if (IsScreenSwitched)
            {
                return;
            }

            base.UpdateScreen(args);
        }

        protected void ProcessRadioMessage(Message m)
        {
            if (m.Data.Compare(Bordmonitor.DataRadioOn))
            {
                Bordmonitor.EnableRadioMenu(); // fixes disabled radio menu to update screen
                return;
            }

            if (!IsEnabled)
            {
                return;
            }

            var isRefresh = m.Data.Compare(Bordmonitor.MessageRefreshScreen.Data);
            if (isRefresh)
            {
                m.ReceiverDescription = "Screen refresh";
                _skipClearTillRefresh = false;
                if (_skipRefreshScreen)
                {
                    _skipRefreshScreen = false;
                    return;
                }
            }
            var isClear = m.Data.Compare(Bordmonitor.MessageClearScreen.Data);
            if (isClear)
            {
                m.ReceiverDescription = "Screen clear";
                if (_skipClearScreen || _skipClearTillRefresh)
                {
                    _skipClearScreen = false;
                    return;
                }
            }
            if (isClear || isRefresh)
            {
                if (IsScreenSwitched)
                {
                    IsScreenSwitched = false;
                }

                if (_disableRadioMenu || isClear)
                {
                    _disableRadioMenu = false;
                    Bordmonitor.DisableRadioMenu();
                    return;
                }

                // TODO test "INFO" button
                UpdateScreen(MenuScreenUpdateReason.Refresh);
                return;
            }

            // Screen switch
            // 0x46 0x01 - switched by nav, after 0x45 0x91 from nav (eg. "menu" button)
            // 0x46 0x02 - switched by radio ("switch" button). 
            if (m.Data.Length == 2 && m.Data[0] == 0x46 && (m.Data[1] == 0x01 || m.Data[1] == 0x02))
            {
                switch (m.Data[1])
                {
                    case 0x01:
                        m.ReceiverDescription = "Screen SW by nav";
                        break;
                    case 0x02:
                        m.ReceiverDescription = "Screen SW by rad";
                        _skipClearScreen = true; // to prevent on "clear screen" update on switch to BC/nav
                        break;
                }
                IsScreenSwitched = true;
                return;
            }

            /*if (m.Data.Compare(Bordmonitor.DataAUX))
            {
                IsScreenSwitched = false;
                UpdateScreen(); // TODO prevent flickering
                return;
            }*/

            if (m.Data.StartsWith(Bordmonitor.DataShowTitle) && (_lastTitle == null || !_lastTitle.Data.Compare(m.Data)))
            {
                IsScreenSwitched = false;
                _disableRadioMenu = true;
                UpdateScreen(MenuScreenUpdateReason.Refresh);
                return;
            }
        }

        protected void ProcessToRadioMessage(Message m)
        {
            if (!IsEnabled)
            {
                return;
            }

            // item click
            if (m.Data.Length == 4 && m.Data.StartsWith(0x31, 0x60, 0x00) && m.Data[3] <= 9)
            {
                var index = GetItemIndex(m.Data[3], true);
                m.ReceiverDescription = "Screen item click #" + index;
                var item = CurrentScreen.GetItem(index);
                if (item != null)
                {
                    item.Click();
                }
                return;
            }

            // BM buttons
            if (m.Data.Length == 2 && m.Data[0] == 0x48)
            {
                switch (m.Data[1])
                {
                    case 0x14: // <>
                        m.ReceiverDescription = "BM button <> - navigate home";
                        NavigateHome();
                        break;
                    case 0x07:
                        m.ReceiverDescription = "BM button Clock - navigate BC";
                        NavigateAfterHome(BordcomputerScreen.Instance);
                        break;
                    case 0x20:
                        m.ReceiverDescription = "BM button Sel"; // - navigate player";
                        // TODO fix in cdc mode
                        //NavigateAfterHome(HomeScreen.Instance.PlayerScreen);
                        break;
                    case 0x30:
                        m.ReceiverDescription = "BM button Switch Screen";
                        /*if (screenSwitched)
                        {
                            UpdateScreen();
                        }*/
                        break;
                    case 0x23:
                        m.ReceiverDescription = "BM button Mode";
                        Bordmonitor.EnableRadioMenu(); // TODO test [and remove]
                        break;
                    case 0x04:
                        m.ReceiverDescription = "BM button Tone";
                        Bordmonitor.EnableRadioMenu(); // TODO test [and remove]
                        break;
                    case 0x10:
                        m.ReceiverDescription = "BM Button < prev track";
                        _mediaEmulator.Player.Prev();
                        break;
                    case 0x00:
                        m.ReceiverDescription = "BM Button > next track";
                        _mediaEmulator.Player.Next();
                        break;
                }
                return;
            }
        }

        bool _isDrawing;
        Message _lastTitle;

        protected override void DrawScreen(MenuScreenUpdateEventArgs args)
        {
            if (_isDrawing)
            {
                return; // TODO test
            }
            lock (_drawLock)
            {
                _isDrawing = true;
                _skipRefreshScreen = true;
                _skipClearTillRefresh = true; // TODO test no screen items lost
                base.DrawScreen(args);

                Bordmonitor.ShowText(CurrentScreen.Status ?? String.Empty, BordmonitorFields.Status);
                _lastTitle = Bordmonitor.ShowText(CurrentScreen.Title ?? String.Empty, BordmonitorFields.Title);
                for (byte i = 0; i < 10; i++)
                {
                    var index = GetItemIndex(i, true);
                    var item = CurrentScreen.GetItem(index);
                    var s = item == null ? String.Empty : item.Text;
                    Bordmonitor.ShowText(s ?? String.Empty, BordmonitorFields.Item, i, item != null && item.IsChecked);
                }
                _skipRefreshScreen = true;
                _skipClearTillRefresh = true;
                Bordmonitor.RefreshScreen();
                _isDrawing = false;
            }
        }

        byte GetItemIndex(byte index, bool back = false)
        {
            if (index > 9)
            {
                index -= 0x40;
            }
            // TODO also try 1-3 & 6-8
            var smallscreenOffset = CurrentScreen.ItemsCount > 6 ? 0 : 2;
            if (back)
            {
                if (index > 2 && index < smallscreenOffset + 3)
                {
                    index += (byte)(3 + smallscreenOffset);
                }
                smallscreenOffset *= -1;
            }
            return (byte)(index <= 2 ? index : index + smallscreenOffset);
        }

        public bool IsScreenSwitched
        {
            get { return _isScreenSwitched; }
            set
            {
                if (_isScreenSwitched == value)
                {
                    return;
                }
                _isScreenSwitched = value;
                if (value)
                {
                    ScreenSuspend();
                }
                else
                {
                    Logger.Info("Screen switched back to radio", "BM");
                    ScreenWakeup();
                }
            }
        }

        #endregion

        public static BordmonitorMenu Instance
        {
            get
            {
                if (_instance == null)
                {
                    //instance = new BordmonitorMenu();
                    throw new Exception("Not inited BM menu");
                }
                return _instance;
            }
        }
    }
}
