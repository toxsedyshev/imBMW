using System;
using Microsoft.SPOT;
using imBMW.Multimedia;
using imBMW.iBus.Devices.Real;
using System.Threading;
using imBMW.Tools;
using imBMW.Features.Menu.Screens;
using imBMW.Features.Menu;
using imBMW.Features.Localizations;
using System.Text;

namespace imBMW.iBus.Devices.Emulators
{
    public class BordmonitorAUX : MediaEmulator
    {
        bool isRadioActive = false;
        bool isAUXSelected = false;

        #region Messages

        static Message MessageAUXHighVolumeP6 = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Set AUX high volume", 0x48, 0x03);

        static byte[] DataCD = new byte[] { 0x23, 0x62, 0x10, 0x43, 0x44 };
        static byte[] DataCDStartPlaying = new byte[] { 0x38, 0x03, 0x00 };
        static byte[] DataBand = new byte[] { 0xA5, 0x62, 0x01, 0x41 };
        static string[] RadioBands = new string[] { "  FM ", " FMD ", " FM1 ", " FM2 ", " LW  ", " LWA ", " MW  ", " MWA ", " SW  ", " SWA " };

        #endregion

        public BordmonitorAUX(IAudioPlayer player)
            : base(player)
        {
            Radio.OnOffChanged += Radio_OnOffChanged;
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
        }

        protected override void MultiFunctionSteeringWheel_ButtonPressed(MFLButton button)
        {
            if (IsEnabled && !mflModeTelephone)
            {
                switch (button)
                {
                    case MFLButton.Next:
                        Next();
                        break;
                    case MFLButton.Prev:
                        Prev();
                        break;
                }
            }

            base.MultiFunctionSteeringWheel_ButtonPressed(button);
        }

        #region AUX

        void Radio_OnOffChanged(bool turnedOn)
        {
            IsRadioActive = turnedOn;
        }

        void ProcessRadioMessage(Message m)
        {
            if (!IsAUXSelected && m.Data.Compare(Bordmonitor.DataAUX))
            {
                IsAUXSelected = true;
            }
            else if (IsAUXSelected && m.Data.StartsWith(DataCDStartPlaying))
            {
                IsAUXSelected = false;
            }
            else if (IsAUXSelected && m.Data.StartsWith(DataBand))
            {
                var band = ASCIIEncoding.GetString(m.Data.Skip(DataBand.Length));
                if (RadioBands.Contains(band))
                {
                    IsAUXSelected = false;
                }
            }
        }

        void CheckAuxOn()
        {
            IsEnabled = IsAUXSelected && IsRadioActive;
        }

        protected override void OnIsEnabledChanged(bool isEnabled, bool fire = true)
        {
            if (isEnabled)
            {
                Player.PlayerHostState = PlayerHostState.On;
                Play();
            }

            base.OnIsEnabledChanged(isEnabled);

            if (!isEnabled)
            {
                Player.PlayerHostState = IsRadioActive ? PlayerHostState.StandBy : PlayerHostState.Off;
                Pause();
            }
        }

        public bool IsRadioActive
        {
            get { return isRadioActive; }
            set
            {
                if (isRadioActive == value)
                {
                    return;
                }
                isRadioActive = value;
                CheckAuxOn();
            }
        }

        public bool IsAUXSelected
        {
            get { return isAUXSelected; }
            set
            {
                if (isAUXSelected == value)
                {
                    return;
                }
                isAUXSelected = value;
                CheckAuxOn();
                if (value)
                {
                    Manager.EnqueueMessage(MessageAUXHighVolumeP6);
                }
            }
        }

        #endregion

    }
}
