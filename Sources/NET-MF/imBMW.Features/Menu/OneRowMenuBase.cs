using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Emulators;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using System.Threading;
using imBMW.Multimedia;
using imBMW.Features.Menu.Screens;

namespace imBMW.Features.Menu
{
    public abstract class OneRowMenuBase : MenuBase
    {
        protected bool mflModeTelephone;

        protected OneRowMenuBase(MediaEmulator mediaEmulator)
            : base(mediaEmulator)
        {
            MultiFunctionSteeringWheel.ButtonPressed += MultiFunctionSteeringWheel_ButtonPressed;
        }

        #region Abstract

        protected abstract bool GetTelephoneModeForNavigation();

        protected abstract void DisplayText(string s, TextAlign align);

        #endregion

        #region Player items

        protected override int StatusTextMaxlen { get { return 11; } }

        protected override void ShowPlayerStatus(IAudioPlayer player, AudioPlayerIsPlayingStatusEventArgs args)
        {
            var status = player.GetPlayingStatusString(args.IsPlaying, StatusTextMaxlen);
            ShowPlayerStatus(player, status);

            if (CurrentScreen == player.Menu)
            {
                args.IsShownOnIKE = true;
            }
        }

        protected override void ShowPlayerStatus(IAudioPlayer player, AudioPlayerStatusEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }

            var status = player.GetStatusString(args.Status, args.Event, StatusTextMaxlen);
            ShowPlayerStatus(player, status);

            if (args.Event == PlayerEvent.Next
                || args.Event == PlayerEvent.Prev
                || args.Event == PlayerEvent.Settings)
            {
                ShowPlayerStatusWithDelay(player);
            }

            if (CurrentScreen == player.Menu)
            {
                args.IsShownOnIKE = true;
            }
        }

        #endregion

        #region Menu control

        int shownItemIndex;

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

        protected bool MflModeTelephone
        {
            set
            {
                if (mflModeTelephone == value)
                {
                    return;
                }
                mflModeTelephone = value;
                OnMFLModeChanged(value);
            }
            get
            {
                return mflModeTelephone;
            }
        }

        protected virtual void OnMFLModeChanged(bool isPhone)
        { }

        private void MultiFunctionSteeringWheel_ButtonPressed(MFLButton button)
        {
            if (!GetTelephoneModeForNavigation())
            {
                return;
            }
            switch (button)
            {
                case MFLButton.ModeRadio:
                    MflModeTelephone = false;
                    return;
                case MFLButton.ModeTelephone:
                    MflModeTelephone = true;
                    return;
            }
            if (IsEnabled && MflModeTelephone)
            {
                switch (button)
                {
                    case MFLButton.Next:
                        ScrollNext();
                        break;
                    case MFLButton.Prev:
                        ScrollPrev();
                        break;
                    case MFLButton.Dial:
                        PressedSelect();
                        break;
                    case MFLButton.DialLong:
                        PressedBack();
                        break;
                }
            }
        }

        protected void ScrollNext()
        {
            ShownItemIndex++;
            UpdateScreen(MenuScreenUpdateReason.Scroll);
        }

        protected void ScrollPrev()
        {
            ShownItemIndex--;
            UpdateScreen(MenuScreenUpdateReason.Scroll);
        }

        protected void PressedSelect()
        {
            UpdateScreen(MenuScreenUpdateReason.Refresh);
            var item = ShownItem;
            if (item != null)
            {
                item.Click();
            }
        }

        protected void PressedBack()
        {
            if (CurrentScreen == HomeScreen.Instance)
            {
                UpdateScreen(MenuScreenUpdateReason.Refresh);
            }
            else
            {
                NavigateBack();
            }
        }

        protected void PressedHome()
        {
            if (CurrentScreen == HomeScreen.Instance)
            {
                UpdateScreen(MenuScreenUpdateReason.Refresh);
            }
            else
            {
                NavigateHome();
            }
        }

        protected override void ScreenNavigatedTo(MenuScreen screen, bool fromAnotherScreen)
        {
            if (fromAnotherScreen)
            {
                ShownItemIndex = 0;
            }

            base.ScreenNavigatedTo(screen, fromAnotherScreen);
        }

        #endregion

        #region Drawing

        protected delegate void DrawHandler(string text, TextAlign align);

        protected delegate void RefreshHandler(MenuScreenUpdateReason reason);

        protected void DrawScreen(MenuScreenUpdateEventArgs args, int maxLength, DrawHandler callback, RefreshHandler refreshCallback = null)
        {
            string showText;
            var align = TextAlign.Left;
            switch (args.Reason)
            {
                case MenuScreenUpdateReason.Navigation:
                    showText = CurrentScreen.Title;
                    if (showText.Length < maxLength)
                    {
                        showText = CharIcons.NetRect + showText;
                    }
                    if (showText.Length < Radio.DisplayTextMaxLength)
                    {
                        showText += CharIcons.NetRect;
                    }
                    align = TextAlign.Center;
                    if (refreshCallback != null)
                    {
                        refreshCallback(MenuScreenUpdateReason.Scroll);
                    }
                    break;
                case MenuScreenUpdateReason.StatusChanged:
                    if (CurrentScreen.Status == String.Empty)
                    {
                        UpdateScreen(MenuScreenUpdateReason.Refresh);
                        return;
                    }
                    showText = CurrentScreen.Status;
                    align = TextAlign.Center;
                    if (refreshCallback != null)
                    {
                        refreshCallback(MenuScreenUpdateReason.Refresh);
                    }
                    break;
                default:
                    showText = GetItemString(args.Reason, maxLength, refreshCallback);
                    break;
            }

            callback(showText, align);
        }

        private string GetItemString(MenuScreenUpdateReason reason, int maxLength, RefreshHandler refreshCallback)
        {
            var showText = GetShownItemString(maxLength, false);

            if (showText.Length <= maxLength)
            {
                return showText;
            }

            var separator = showText.IndexOf(": ");
            if (separator <= 0 || separator == showText.Length - 2)
            {
                return GetShownItemString(maxLength, true);
            }

            if (showText.Length == maxLength + 1)
            {
                return showText.Substring(0, separator + 1) + showText.Substring(separator + 2);
            }

            if (reason == MenuScreenUpdateReason.Scroll)
            {
                if (refreshCallback != null)
                {
                    refreshCallback(MenuScreenUpdateReason.Refresh);
                }
                return showText.Substring(0, separator + 1);
            }
            else
            {
                showText = showText.Substring(separator + 2);
                if (ShownItem != null && !StringHelpers.IsNullOrEmpty(ShownItem.RadioAbbreviation))
                {
                    var len = showText.Length + ShownItem.RadioAbbreviation.Length;
                    if (len < maxLength)
                    {
                        return ShownItem.RadioAbbreviation + " " + showText;
                    }
                    else if (len == maxLength
                        && (ShownItem.RadioAbbreviation[ShownItem.RadioAbbreviation.Length - 1] == ':'
                            || !showText[0].IsLetterOrNumber()))
                    {
                        return ShownItem.RadioAbbreviation + showText;
                    }
                }
            }

            return showText;
        }

        private string GetShownItemString(int maxLength, bool useAbbr)
        {
            var item = ShownItem;
            if (item == null)
            {
                return CurrentScreen.Title;
            }
            var s = item.Text;
            if (useAbbr && !StringHelpers.IsNullOrEmpty(item.RadioAbbreviation)
                && (s.Length > maxLength 
                    || item.Type == MenuItemType.Checkbox && s.Length + 1 > maxLength))
            {
                s = item.RadioAbbreviation;
            }
            if (item.Type == MenuItemType.Checkbox)
            {
                s = (item.IsChecked ? '*' : CharIcons.Bull) + s;
            }
            return s;
        }

        #endregion
    }
}
