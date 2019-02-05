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
    public class RadioMenu : OneRowMenuBase
    {
        static RadioMenu instance;
        
        private readonly byte[] DataMIDAudioButton = new byte[] { 0x20, 0x01, 0xB0, 0x00 };
        private readonly byte[] DataMIDCDC = new byte[] { 0x23, 0xC0, 0x20, 0x43, 0x44, 0x20, 0x31, 0x2D, 0x30, 0x31 };
        private readonly byte[] DataMIDCDCFirstButtons = new byte[] { 0x21, 0x00, 0x00, 0x60, 0x20, 0x31, 0x20, 0x05, 0x20, 0x32, 0x20, 0x05, 0x20, 0x33, 0x20, 0x05, 0x20, 0x34, 0x20, 0x05, 0x20, 0x35, 0x20, 0x05, 0x20, 0x36, 0x20 };
        private readonly byte[] DataMIDAUXFirstButtons = new byte[] { 0x21, 0x00, 0x00, 0x60, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x20 };

        private readonly Message MessageMIDMenuButtons = new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "MID menu buttons", 0x21, 0x00, 0x00, 0x60, 0x20, (byte)'S', (byte)'E', (byte)'L', 0x05, (byte)'E', (byte)'C', (byte)'T', 0x20, 0x05, 0xAE, (byte)'S', (byte)'C', (byte)'R', 0x05, (byte)'O', (byte)'L', (byte)'L', 0xAD, 0x05, 0xCA, (byte)'B', (byte)'A', (byte)'C', 0x05, (byte)'K', 0x20, 0xC0, 0xCA);
        private Message MessageMIDCDCLastButtons = new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "MID menu last buttons", 0x21, 0x00, 0x00, 0x06, 0x46, 0x4D, 0x05, 0x41, 0x4D, 0x05, 0x54, 0x50, 0x20, 0x05, 0x20, 0x52, 0x4E, 0x44, 0x05, 0x53, 0x43, 0x20, 0x05, 0x4D, 0x4F, 0x44, 0x45);
        private Message MessageMIDAUXLastButtons = new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "MID AUX menu last buttons", 0x21, 0x00, 0x00, 0x06, 0x46, 0x4D, 0x05, 0x41, 0x4D, 0x05, 0x54, 0x50, 0x20, 0x05, 0x20, 0x05, 0x20, 0x05, 0x4D, 0x4F, 0x44, 0x45);
        private readonly int[] MaskMIDCDCLastButtons = new int[] { 12, 14, 21 };
        private readonly int[] MaskMIDAUXLastButtons = new int[] { 12 };

        private bool wereMIDButtonsOverriden;
        private bool waitingBatchMIDUpdate;

        public bool TelephoneModeForNavigation { get; set; }

        private RadioMenu(MediaEmulator mediaEmulator)
            : base(mediaEmulator)
        {
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

        #region Menu control

        protected override bool GetTelephoneModeForNavigation()
        {
            return TelephoneModeForNavigation;
        }

        protected override void OnMFLModeChanged(bool isPhone)
        {
            if (IsEnabled)
            {
                DisplayText(CharIcons.SelectedArrow + (isPhone ? "Navigation" : "Playback"), TextAlign.Left);
                RefreshScreenWithDelay(MenuScreenUpdateReason.Scroll);
            }
        }

        private void ProcessMIDToRadioMessage(Message m)
        {
            if (m.Data.Compare(DataMIDAudioButton))
            {
                IsEnabled = false;
                m.ReceiverDescription = "MID Audio Button";
            }

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

            if (Radio.HasMID 
                && (m.DestinationDevice == DeviceAddress.MultiInfoDisplay
                    || m.DestinationDevice == DeviceAddress.Broadcast))
            {
                if (!EmulatorIsMIDAUX) // MID CDC
                {
                    if (m.Data.StartsWith(DataMIDCDC))
                    {
                        SetIsEnabled(true, false);
                        wereMIDButtonsOverriden = false;
                        UpdateScreen(MenuScreenUpdateReason.Refresh);
                        m.ReceiverDescription = "CD 1-01";
                    }
                    else if (m.Data.Compare(DataMIDCDCFirstButtons))
                    {
                        if (IsEnabled && !waitingBatchMIDUpdate)
                        {
                            wereMIDButtonsOverriden = false;
                        }
                        m.ReceiverDescription = "Disk change buttons display";
                    }
                    else if (m.Data.Compare(MaskMIDCDCLastButtons, MessageMIDCDCLastButtons.Data))
                    {
                        m.ReceiverDescription = MessageMIDCDCLastButtons.ReceiverDescription;
                        if (IsEnabled)
                        {
                            MessageMIDCDCLastButtons = m; // to save statuses of TP, RND and SC flags
                            if (!wereMIDButtonsOverriden && !waitingBatchMIDUpdate)
                            {
                                wereMIDButtonsOverriden = true;
                                Manager.EnqueueMessage(MessageMIDMenuButtons, m);
                            }
                        }
                    }
                }
                else // MID AUX
                {
                    if (m.Data.Compare(MIDAUX.DataDisplayAUX) || m.Data.Compare(MIDAUX.DataDisplayAUX2))
                    {
                        SetIsEnabled(true, false);
                        wereMIDButtonsOverriden = false;
                        UpdateScreen(MenuScreenUpdateReason.Refresh);
                        m.ReceiverDescription = "MID: AUX";
                    }
                    else if (m.Data.Compare(DataMIDAUXFirstButtons))
                    {
                        if (IsEnabled && !waitingBatchMIDUpdate)
                        {
                            wereMIDButtonsOverriden = false;
                        }
                        m.ReceiverDescription = "AUX empty buttons display";
                    }
                    else if (m.Data.Compare(MaskMIDAUXLastButtons, MessageMIDAUXLastButtons.Data))
                    {
                        m.ReceiverDescription = MessageMIDAUXLastButtons.ReceiverDescription;
                        if (IsEnabled)
                        {
                            MessageMIDAUXLastButtons = m; // to save status of TP flag
                            if (!wereMIDButtonsOverriden && !waitingBatchMIDUpdate)
                            {
                                wereMIDButtonsOverriden = true;
                                Manager.EnqueueMessage(MessageMIDMenuButtons, m);
                            }
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

        private void CancelRefreshScreenWithDelay()
        {
            if (refreshScreenDelayTimer != null)
            {
                refreshScreenDelayTimer.Dispose();
                refreshScreenDelayTimer = null;
            }
        }

        private void RefreshScreenWithDelay(MenuScreenUpdateReason reason = MenuScreenUpdateReason.Refresh)
        {
            CancelRefreshScreenWithDelay();
            refreshScreenDelayTimer = new Timer(delegate
            {
                UpdateScreen(reason);
            }, null, refreshScreenDelay, 0);
        }

        protected override void DrawScreen(MenuScreenUpdateEventArgs args)
        {
            CancelRefreshScreenWithDelay();
            DrawScreen(args, Radio.DisplayTextMaxLength, (text, align) =>
            {
                bool sendMIDButtons = false;
                if (Radio.HasMID && !wereMIDButtonsOverriden)
                {
                    sendMIDButtons = true;
                    waitingBatchMIDUpdate = true;
                }
                Radio.DisplayTextWithDelay(text, align, () =>
                {
                    if (sendMIDButtons)
                    {
                        wereMIDButtonsOverriden = true;
                        waitingBatchMIDUpdate = false;
                        var midButtons = new Message[] { MessageMIDMenuButtons, EmulatorIsMIDAUX ? MessageMIDAUXLastButtons : MessageMIDCDCLastButtons };
                        Manager.EnqueueMessage(midButtons);
                    }
                });
            }, reason =>
            {
                RefreshScreenWithDelay(reason);
            });

            if (IsDuplicateOnIKEEnabled)
            {
                DrawScreen(args, InstrumentClusterElectronics.DisplayTextMaxLength, (text, align) =>
                {
                    InstrumentClusterElectronics.DisplayTextWithDelay(text, align);
                });
            }
        }
        
        protected override void DisplayText(string s, TextAlign align)
        {
            Radio.DisplayText(s, align);
            if (IsDuplicateOnIKEEnabled)
            {
                InstrumentClusterElectronics.DisplayText(s, align);
            }
        }

        #endregion
    }
}
