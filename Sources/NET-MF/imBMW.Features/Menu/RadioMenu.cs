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

        private readonly byte[] DataMIDCDC = new byte[] { 0x23, 0xC0, 0x20, 0x43, 0x44, 0x20, 0x31, 0x2D, 0x30, 0x31 };
        private readonly byte[] DataMIDCDCFirstButtons = new byte[] { 0x21, 0x00, 0x00, 0x60, 0x20, 0x31, 0x20, 0x05, 0x20, 0x32, 0x20, 0x05, 0x20, 0x33, 0x20, 0x05, 0x20, 0x34, 0x20, 0x05, 0x20, 0x35, 0x20, 0x05, 0x20, 0x36, 0x20 };
        private readonly byte[] DataMIDAUXFirstButtons = new byte[] { 0x21, 0x00, 0x00, 0x60, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x20 };

        private readonly Message MessageMIDMenuButtons = new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "MID menu buttons", 0x21, 0x00, 0x00, 0x60, 0x20, (byte)'S', (byte)'E', (byte)'L', 0x05, (byte)'E', (byte)'C', (byte)'T', 0x20, 0x05, 0xAE, (byte)'S', (byte)'C', (byte)'R', 0x05, (byte)'O', (byte)'L', (byte)'L', 0xAD, 0x05, 0xCA, (byte)'B', (byte)'A', (byte)'C', 0x05, (byte)'K', 0x20, 0xC0, 0xCA);
        private Message MessageMIDCDCLastButtons = new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "MID menu last buttons", 0x21, 0x00, 0x00, 0x06, 0x46, 0x4D, 0x05, 0x41, 0x4D, 0x05, 0x54, 0x50, 0x20, 0x05, 0x20, 0x52, 0x4E, 0x44, 0x05, 0x53, 0x43, 0x20, 0x05, 0x4D, 0x4F, 0x44, 0x45);
        private Message MessageMIDAUXLastButtons = new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "MID AUX menu last buttons", 0x21, 0x00, 0x00, 0x06, 0x46, 0x4D, 0x05, 0x41, 0x4D, 0x05, 0x54, 0x50, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x4D, 0x4F, 0x44, 0x45);
        private readonly int[] MaskMIDCDCLastButtons = new int[] { 12, 14, 21 };
        private readonly int[] MaskMIDAUXLastButtons = new int[] { 12 };

        private bool mflModeTelephone;
        private bool wereMIDButtonsOverriden;

        public bool TelephoneModeForNavigation { get; set; }

        private RadioMenu(MediaEmulator mediaEmulator)
            : base(mediaEmulator)
        {
            MultiFunctionSteeringWheel.ButtonPressed += MultiFunctionSteeringWheel_ButtonPressed;
            if (Radio.HasMID)
            {
                Manager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.MultiInfoDisplay, DeviceAddress.Radio, ProcessMIDToRadioMessage);
            }
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

        private bool MflModeTelephone
        {
            set
            {
                if (mflModeTelephone == value)
                {
                    return;
                }
                mflModeTelephone = value;
                if (IsEnabled)
                {
                    DisplayText(CharIcons.SelectedArrow + (value ? "Navigation" : "Playback"), TextAlign.Center);
                    RefreshScreenWithDelay(MenuScreenUpdateReason.Scroll);
                }
            }
            get
            {
                return mflModeTelephone;
            }
        }

        void MultiFunctionSteeringWheel_ButtonPressed(MFLButton button)
        {
            if (!TelephoneModeForNavigation)
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

        private void ProcessMIDToRadioMessage(Message m)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (m.Data.Length == 4 && m.Data[0] == 0x31 && m.Data[1] == 0x00 && m.Data[2] == 0x00)
            {
                switch (m.Data[3])
                {
                    case 0x00:
                    case 0x01:
                        PressedSelect();
                        break;
                    case 0x02:
                        ScrollPrev();
                        break;
                    case 0x03:
                        ScrollNext();
                        break;
                    case 0x04:
                        PressedBack();
                        break;
                    case 0x05:
                        PressedHome();
                        break;
                }
            }
        }

        protected override void ProcessRadioMessage(Message m)
        {
            base.ProcessRadioMessage(m);

            if (!IsEnabled)
            {
                return;
            }

            if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x06)
            {
                // switch cd buttons:
                //   2 - select
                //
                //   3 - prev
                //   4 - next
                //
                //   5 - back
                //   6 - home
                byte cdNumber = m.Data[2];
                if (!Radio.HasMID)
                {
                    switch (cdNumber)
                    {
                        case 0x02:
                            PressedSelect();
                            break;
                        case 0x03:
                            ScrollPrev();
                            break;
                        case 0x04:
                            ScrollNext();
                            break;
                        case 0x05:
                            PressedBack();
                            break;
                        case 0x06:
                            PressedHome();
                            break;
                    }
                }
                m.ReceiverDescription = "Change CD: " + cdNumber;
            }
            // TODO bind rnd, scan

            if (Radio.HasMID && m.DestinationDevice == DeviceAddress.MultiInfoDisplay)
            {
                if (!EmulatorIsMIDAUX) // MID CDC
                {
                    if (m.Data.StartsWith(DataMIDCDC))
                    {
                        SetIsEnabled(true, false);
                        UpdateScreen(MenuScreenUpdateReason.Refresh);
                        wereMIDButtonsOverriden = false;
                        m.ReceiverDescription = "CD 1-01";
                    }
                    else if (m.Data.Compare(DataMIDCDCFirstButtons))
                    {
                        wereMIDButtonsOverriden = false;
                        m.ReceiverDescription = "Disk change buttons display";
                    }
                    else if (m.Data.Compare(MaskMIDCDCLastButtons, MessageMIDCDCLastButtons.Data))
                    {
                        m.ReceiverDescription = MessageMIDCDCLastButtons.ReceiverDescription;
                        MessageMIDCDCLastButtons = m; // to save statuses of TP, RND and SC flags
                        if (!wereMIDButtonsOverriden)
                        {
                            Manager.EnqueueMessage(MessageMIDMenuButtons, m);
                            wereMIDButtonsOverriden = true;
                        }
                        else
                        {
                            wereMIDButtonsOverriden = false;
                        }
                    }
                }
                else // MID AUX
                {
                    if (m.Data.Compare(MIDAUX.DataDisplayAUX) || m.Data.Compare(MIDAUX.DataDisplayAUX2))
                    {
                        SetIsEnabled(true, false);
                        UpdateScreen(MenuScreenUpdateReason.Refresh);
                        wereMIDButtonsOverriden = false;
                        m.ReceiverDescription = "MID: AUX";
                    }
                    else if (m.Data.Compare(DataMIDAUXFirstButtons))
                    {
                        wereMIDButtonsOverriden = false;
                        m.ReceiverDescription = "AUX empty buttons display";
                    }
                    else if (m.Data.Compare(MaskMIDAUXLastButtons, MessageMIDAUXLastButtons.Data))
                    {
                        m.ReceiverDescription = MessageMIDAUXLastButtons.ReceiverDescription;
                        MessageMIDAUXLastButtons = m; // to save status of TP flag
                        if (!wereMIDButtonsOverriden)
                        {
                            Manager.EnqueueMessage(MessageMIDMenuButtons, m);
                            wereMIDButtonsOverriden = true;
                        }
                        else
                        {
                            wereMIDButtonsOverriden = false;
                        }
                    }
                }
            }
            else if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x0A)
            {
                if (CurrentScreen != mediaEmulator.Player.Menu)
                {
                    UpdateScreen(MenuScreenUpdateReason.Refresh);
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

        private void PressedHome()
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

        public bool EmulatorIsMIDAUX
        {
            get
            {
                return mediaEmulator is MIDAUX;
            }
        }

        public bool IsDuplicateOnIKEEnabled { get; set; }

        #endregion

        #region Drawing members

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
                    if (showText.Length < Radio.DisplayTextMaxLen)
                    {
                        showText = CharIcons.NetRect + showText;
                    }
                    if (showText.Length < Radio.DisplayTextMaxLen)
                    {
                        showText += CharIcons.NetRect;
                    }
                    align = TextAlign.Center;
                    RefreshScreenWithDelay(MenuScreenUpdateReason.Scroll);
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

            Message[] midButtons = null;
            if (Radio.HasMID && !wereMIDButtonsOverriden)
            {
                midButtons = new Message[] { MessageMIDMenuButtons, EmulatorIsMIDAUX ? MessageMIDAUXLastButtons : MessageMIDCDCLastButtons };
                wereMIDButtonsOverriden = true;
            }
            DisplayTextWithDelay(showText, align, midButtons);
        }

        private void DisplayText(string s, TextAlign align)
        {
            Radio.DisplayText(s, align);
            if (IsDuplicateOnIKEEnabled)
            {
                InstrumentClusterElectronics.ShowText(s, align);
            }
        }

        private void DisplayTextWithDelay(string s, TextAlign align, Message[] messageSendAfter)
        {
            Radio.DisplayTextWithDelay(s, align, messageSendAfter);
            if (IsDuplicateOnIKEEnabled)
            {
                InstrumentClusterElectronics.ShowText(s, align);
            }
        }

        protected override void ScreenWakeup()
        {
            wereMIDButtonsOverriden = false;
            base.ScreenWakeup();
        }

        protected void CancelRefreshScreenWithDelay()
        {
            if (refreshScreenDelayTimer != null)
            {
                refreshScreenDelayTimer.Dispose();
                refreshScreenDelayTimer = null;
            }
        }

        protected void RefreshScreenWithDelay(MenuScreenUpdateReason reason = MenuScreenUpdateReason.Refresh)
        {
            CancelRefreshScreenWithDelay();
            refreshScreenDelayTimer = new Timer(delegate
            {
                UpdateScreen(reason);
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
                s = (item.IsChecked ? '*' : CharIcons.Bull) + s;
            }
            return s;
        }

        #endregion
    }
}
