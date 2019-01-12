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
        
        public static byte[] DataDisplayAUX = new byte[] { 0x23, 0x00, 0x20, (byte)'A', (byte)'U', (byte)'X', 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        public static byte[] DataDisplayAUX2 = new byte[] { 0x23, 0x00, 0x20, 0x07, 0x20, 0x20, 0x20, 0x20, 0x20, 0x08, 0x41, 0x55, 0x58, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

        #endregion

        public MIDAUX(IAudioPlayer player)
            : base(player)
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
            Manager.AfterMessageSent += Manager_AfterMessageSent;
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        private void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            if (e.CurrentIgnitionState == IgnitionState.Off
                && e.PreviousIgnitionState != IgnitionState.Off)
            {
                IsRadioActive = false;
            }
        }

        private void Manager_AfterMessageSent(MessageEventArgs e)
        {
            var msg = e.Message;
            if (msg.SourceDevice == DeviceAddress.Radio
                && msg.DestinationDevice == DeviceAddress.MultiInfoDisplay
                && IsDisplayMessage(msg.Data))
            {
                MessageDisplayLast = msg;
            }
        }

        bool IsDisplayMessage(byte[] data)
        {
            return data.Length >= 3 && data[0] == 0x23 && data[2] == 0x20
                && (data[1] == 0x00 || data[1] == 0x40 || data[1] == 0xC0);
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
        
        void ProcessRadioMessage(Message m)
        {
            if (IsDisplayMessage(m.Data))
            {
                IsRadioActive = m.Data.Length > 3;

                if (!IsAUXSelected)
                {
                    if (m.Data.Compare(DataDisplayAUX) || m.Data.Compare(DataDisplayAUX2))
                    {
                        IsAUXSelected = true;
                    }
                }
                else
                {
                    if (!m.Data.Compare(DataDisplayAUX) 
                        && !m.Data.Compare(DataDisplayAUX2) 
                        && !m.Compare(MessageDisplayLast))
                    {
                        IsAUXSelected = false;
                    }
                }
                //m.ReceiverDescription = m.DataDump + ASCIIEncoding.GetString(m.Data, 0, -1, true);
            }
        }

        private void ProcessToRadioMessage(Message m)
        {
            if (!IsEnabled)
            {
                return;
            }
            
            if (m.Data.Length == 4 && m.Data[0] == 0x31 && m.Data[1] == 0x00 && m.Data[2] == 0x00)
            {
                switch (m.Data[3])
                {
                    case 0x0D:
                        m.ReceiverDescription = "MID Button > Next Track";
                        Next();
                        break;
                    case 0x0C:
                        m.ReceiverDescription = "MID Button < Prev Track";
                        Prev();
                        break;
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
                Logger.Info("Radio" + (value ? "" : " not") + " active");
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
                Logger.Info("AUX" + (value ? "" : " not") + " selected");
                CheckAuxOn();
            }
        }

        #endregion

    }
}
