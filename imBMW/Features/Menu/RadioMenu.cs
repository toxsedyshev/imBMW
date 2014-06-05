using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Emulators;
using imBMW.iBus;
using imBMW.Multimedia;
using imBMW.Features.Localizations;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using System.Threading;
using imBMW.Features.Menu.Screens;

namespace imBMW.Features.Menu
{
    public class RadioMenu : MenuBase
    {
        static RadioMenu instance;

        private RadioMenu(MediaEmulator mediaEmulator)
            : base(mediaEmulator)
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
        }

        public static RadioMenu Init(MediaEmulator mediaEmulator)
        {
            if (instance != null)
            {
                // TODO implement hot switch of emulators
                throw new Exception("Already inited");
            }
            instance = new RadioMenu(mediaEmulator);
            return instance;
        }

        #region Player items

        protected override int StatusTextMaxlen { get { return 11; } }

        protected override void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        {
            var s = TextWithIcon(isPlaying ? CharIcons.Play : CharIcons.Pause, player.Name);
            ShowPlayerStatus(player, s);
        }

        protected override void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent)
        {
            if (!IsEnabled)
            {
                return;
            }
            bool showAfterWithDelay = false;
            if (StringHelpers.IsNullOrEmpty(status))
            {
                status = player.Name;
            }
            switch (playerEvent)
            {
                case PlayerEvent.Next:
                    status = TextWithIcon(CharIcons.Next, status);
                    showAfterWithDelay = true;
                    break;
                case PlayerEvent.Prev:
                    status = TextWithIcon(CharIcons.Prev, status);
                    showAfterWithDelay = true;
                    break;
                case PlayerEvent.Playing:
                    status = TextWithIcon(CharIcons.Play, status);
                    break;
                case PlayerEvent.Current:
                    status = TextWithIcon(CharIcons.SelectedArrow, status);
                    break;
                case PlayerEvent.Voice:
                    status = TextWithIcon(CharIcons.Voice, status);
                    break;
            }
            ShowPlayerStatus(player, status);
            if (showAfterWithDelay)
            {
                ShowPlayerStatusWithDelay(player);
            }
        }

        #endregion

        #region Menu control

        int shownItemIndex;
        //bool itemScrolled;

        public int ShownItemIndex
        {
            get
            {
                if (shownItemIndex < 0 || shownItemIndex >= CurrentScreen.ItemsCount)
                {
                    shownItemIndex = 0;
                }
                var item = CurrentScreen.GetItem(shownItemIndex);
                if (CurrentScreen.ItemsCount > 1 && (item == null || item.Action == MenuItemAction.GoBackScreen))
                {
                    shownItemIndex++;
                    return ShownItemIndex;
                }
                return shownItemIndex;
            }
            set { shownItemIndex = value; }
        }

        public MenuItem ShownItem
        {
            get
            {
                return CurrentScreen.GetItem(ShownItemIndex);
            }
        }

        void ProcessRadioMessage(Message m)
        {
            if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x06)
            {
                //delayUpdateScreen = true;
                // switch cd buttons:
                //   2 - select
                //
                //   3 - prev
                //   4 - next
                //
                //   5 - back
                //   6 - home
                byte cdNumber = m.Data[2];
                switch (cdNumber)
                {
                    case 0x02:
                        UpdateScreen(MenuScreenUpdateReason.Refresh);
                        var item = ShownItem;
                        if (item != null)
                        {
                            item.Click();
                        }
                        break;
                    case 0x03:
                        ShownItemIndex--;
                        //itemScrolled = true;
                        UpdateScreen(MenuScreenUpdateReason.Scroll);
                        break;
                    case 0x04:
                        ShownItemIndex++;
                        //itemScrolled = true;
                        UpdateScreen(MenuScreenUpdateReason.Scroll);
                        break;
                    case 0x05:
                        NavigateBack();
                        if (CurrentScreen == HomeScreen.Instance)
                        {
                            UpdateScreen(MenuScreenUpdateReason.Refresh);
                        }
                        break;
                    case 0x06:
                        NavigateHome();
                        if (CurrentScreen == HomeScreen.Instance)
                        {
                            UpdateScreen(MenuScreenUpdateReason.Refresh);
                        }
                        break;
                }
            }
            else if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x0A)
            {
                //delayUpdateScreen = true;
                switch (m.Data[2])
                {
                    case 0x00:
                        mediaEmulator.Player.Next();
                        break;
                    case 0x01:
                        mediaEmulator.Player.Prev();
                        break;
                }
            }
            // TODO bind rnd, scan
        }

        protected override void ScreenNavigatedTo(MenuScreen screen)
        {
            ShownItemIndex = 0;

            base.ScreenNavigatedTo(screen);
        }

        #endregion

        #region Drawing members

        //bool delayUpdateScreen;
        Timer refreshScreenDelayTimer;
        const int refreshScreenDelay = 1000;

        protected override void DrawScreen(MenuScreenUpdateEventArgs args)
        {
            CancelRefreshScreenWithDelay();
            string showText;
            TextAlign align = TextAlign.Left;
            switch (args.Reason)
            {
                case MenuScreenUpdateReason.Navigation:
                    showText = CurrentScreen.Title;
                    align = TextAlign.Center;
                    RefreshScreenWithDelay();
                    break;
                case MenuScreenUpdateReason.StatusChanged:
                    if (CurrentScreen.Status == String.Empty)
                    {
                        UpdateScreen(MenuScreenUpdateReason.Refresh);
                        return;
                    }
                    showText = CurrentScreen.Status;
                    align = TextAlign.Center;
                    RefreshScreenWithDelay();
                    break;
                default:
                    showText = GetShownItemString();
                    var separator = showText.IndexOf(": ");
                    if (separator >= 0)
                    {
                        if (args.Reason == MenuScreenUpdateReason.Scroll)
                        {
                            showText = showText.Substring(0, separator + 1);
                            RefreshScreenWithDelay();
                        }
                        else
                        {
                            showText = showText.Substring(separator + 2);
                        }
                    }
                    break;
            }
            //if (delayUpdateScreen)
            //{
                Radio.DisplayTextWithDelay(showText, align);
            //}
            //else
            //{
            //    Radio.DisplayText(showText, align);
            //}
            //delayUpdateScreen = false;
            //itemScrolled = false;
        }

        protected void CancelRefreshScreenWithDelay()
        {
            if (refreshScreenDelayTimer != null)
            {
                refreshScreenDelayTimer.Dispose();
                refreshScreenDelayTimer = null;
            }
        }

        protected void RefreshScreenWithDelay()
        {
            CancelRefreshScreenWithDelay();
            refreshScreenDelayTimer = new Timer(delegate
            {
                UpdateScreen(MenuScreenUpdateReason.Refresh);
            }, null, refreshScreenDelay, 0);
        }

        protected string GetShownItemString()
        {
            var item = ShownItem;
            if (item == null)
            {
                return CurrentScreen.Title;
            }
            var s = item.Text;
            if (item.Type == MenuItemType.Checkbox)
            {
                s = (item.IsChecked ? '*' : '\x19') + s;
            }
            return s;
        }

        #endregion
    }
}
