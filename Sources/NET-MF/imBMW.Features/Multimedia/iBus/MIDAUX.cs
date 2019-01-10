using System;
using Microsoft.SPOT;
using imBMW.Multimedia;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using System.Text;

namespace imBMW.iBus.Devices.Emulators
{
    public class MIDAUX : MediaEmulator
    {
        bool isRadioActive = false;
        bool isAUXSelected = false;

        #region Messages

        static Message MessageDisplayLast;
        
        static byte[] DataDisplay = new byte[] { 0x23, 0x40, 0x20 };
        static byte[] DataDisplayAUX = new byte[] { 0x23, 0x40, 0x20, (byte)'A', (byte)'U', (byte)'X', 0x20 };

        #endregion

        public MIDAUX(IAudioPlayer player)
            : base(player)
        {
            Radio.OnOffChanged += Radio_OnOffChanged;
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
            Manager.AfterMessageSent += Manager_AfterMessageSent;
        }

        private void Manager_AfterMessageSent(MessageEventArgs e)
        {
            if (e.Message.SourceDevice == DeviceAddress.Radio
                && e.Message.DestinationDevice == DeviceAddress.MultiInfoDisplay
                && e.Message.Data.StartsWith(DataDisplay))
            {
                MessageDisplayLast = e.Message;
            }
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
            // TODO check with CD53
            IsRadioActive = turnedOn;
        }

        void ProcessRadioMessage(Message m)
        {
            if (!IsAUXSelected)
            {
                if (m.Data.Compare(DataDisplayAUX))
                {
                    IsAUXSelected = true;
                }
            }
            else
            {
                if (!m.Data.Compare(DataDisplayAUX) && !m.Compare(MessageDisplayLast))
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
            }
        }

        #endregion

    }
}
