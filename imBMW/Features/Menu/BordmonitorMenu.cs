using System;
using Microsoft.SPOT;
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
        static BordmonitorMenu instance;

        MediaEmulator mediaEmulator;

        bool skipRefreshScreen;
        bool skipClearScreen;
        bool skipClearTillRefresh;
        bool disableRadioMenu;
        bool isScreenSwitched;
        object drawLock = new object();

        private BordmonitorMenu(MediaEmulator mediaEmulator)
        {
            this.mediaEmulator = mediaEmulator;
            mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            mediaEmulator.PlayerIsPlayingChanged += ShowPlayerStatus;
            mediaEmulator.PlayerStatusChanged += ShowPlayerStatus;
            mediaEmulator.PlayerChanged += mediaEmulator_PlayerChanged;
            mediaEmulator_PlayerChanged(mediaEmulator.Player);

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
        }

        public static void Init(MediaEmulator mediaEmulator)
        {
            if (instance != null)
            {
                // TODO implement hot switch of emulators
                throw new Exception("Already inited");
            }
            instance = new BordmonitorMenu(mediaEmulator);
        }

        #region Player items

        void ShowPlayerStatus(IAudioPlayer player)
        {
            ShowPlayerStatus(player, player.IsPlaying);
        }

        void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        {
            string s = isPlaying ? Localization.Current.Playing : Localization.Current.Paused;
            ShowPlayerStatus(player, s);
        }

        Timer displayTextDelayTimer;
        const int displayTextDelay = 2000;
        const int statusTextMaxlen = 11;

        void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent)
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
                    status = TextWithIcon("* ", status);
                    break;
            }
            ShowPlayerStatus(player, status);
            if (showAfterWithDelay)
            {
                ShowPlayerStatusWithDelay(player);
            }
        }

        string TextWithIcon(string icon, string text = null)
        {
            if (text == null)
            {
                text = "";
            }
            if (icon.Length + text.Length < statusTextMaxlen)
            {
                return icon + " " + text;
            }
            else
            {
                return icon + text;
            }
        }

        void ShowPlayerStatus(IAudioPlayer player, string status)
        {
            if (!IsEnabled)
            {
                return;
            }
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }

            player.Menu.Status = status;
        }

        public void ShowPlayerStatusWithDelay(IAudioPlayer player)
        {
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }

            displayTextDelayTimer = new Timer(delegate
            {
                ShowPlayerStatus(player);
            }, null, displayTextDelay, 0);
        }

        void mediaEmulator_PlayerChanged(IAudioPlayer player)
        {
            HomeScreen.Instance.PlayerScreen = player.Menu;
        }

        void mediaEmulator_IsEnabledChanged(MediaEmulator emulator, bool isEnabled)
        {
            IsEnabled = isEnabled;
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

            disableRadioMenu = true;
        }

        public override void UpdateScreen()
        {
            if (IsScreenSwitched)
            {
                return;
            }

            base.UpdateScreen();
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
                skipClearTillRefresh = false;
                if (skipRefreshScreen)
                {
                    skipRefreshScreen = false;
                    return;
                }
            }
            var isClear = m.Data.Compare(Bordmonitor.MessageClearScreen.Data);
            if (isClear)
            {
                m.ReceiverDescription = "Screen clear";
                if (skipClearScreen || skipClearTillRefresh)
                {
                    skipClearScreen = false;
                    return;
                }
            }
            if (isClear || isRefresh)
            {
                if (IsScreenSwitched)
                {
                    IsScreenSwitched = false;
                }

                if (disableRadioMenu || isClear)
                {
                    disableRadioMenu = false;
                    Bordmonitor.DisableRadioMenu();
                    return;
                }

                // TODO test "INFO" button
                UpdateScreen();
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
                        skipClearScreen = true; // to prevent on "clear screen" update on switch to BC/nav
                        break;
                }
                IsScreenSwitched = true;
                return;
            }

            if (m.Data.Compare(Bordmonitor.DataAUX))
            {
                IsScreenSwitched = false;
                UpdateScreen(); // TODO prevent flickering
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
                        m.ReceiverDescription = "BM button <>";
                        NavigateHome();
                        break;
                    case 0x07:
                        m.ReceiverDescription = "BM button Clock";
                        NavigateAfterHome(BordcomputerScreen.Instance);
                        break;
                    case 0x20:
                        m.ReceiverDescription = "BM button Sel";
                        NavigateAfterHome(HomeScreen.Instance.PlayerScreen);
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
                }
                return;
            }
        }

        //Message resendMessage;

        protected override void DrawScreen()
        {
            lock (drawLock)
            {
                skipRefreshScreen = true;
                skipClearTillRefresh = true; // TODO test no screen items lost
                base.DrawScreen();

                Bordmonitor.ShowText(CurrentScreen.Status ?? String.Empty, BordmonitorFields.Status);
                Bordmonitor.ShowText(CurrentScreen.Title ?? String.Empty, BordmonitorFields.Title);
                for (byte i = 0; i < 10; i++)
                {
                    var index = GetItemIndex(i, true);
                    var item = CurrentScreen.GetItem(index);
                    var s = item == null ? String.Empty : item.Text;
                    Bordmonitor.ShowText(s ?? String.Empty, BordmonitorFields.Item, i, item != null && item.IsChecked);
                }
                skipRefreshScreen = true;
                skipClearTillRefresh = true;
                Bordmonitor.RefreshScreen();
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
            get { return isScreenSwitched; }
            set
            {
                if (isScreenSwitched == value)
                {
                    return;
                }
                isScreenSwitched = value;
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
                if (instance == null)
                {
                    //instance = new BordmonitorMenu();
                    throw new Exception("Not inited BM menu");
                }
                return instance;
            }
        }
    }
}
