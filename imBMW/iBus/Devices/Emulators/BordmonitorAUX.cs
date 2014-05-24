using imBMW.Multimedia;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Emulators
{
    public class BordmonitorAUX : MediaEmulator
    {
        bool _isRadioActive;
        bool _isAUXSelected;

        #region Messages

        static readonly Message MessageAUXHighVolumeP6 = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Set AUX high volume", 0x48, 0x03);

        static readonly byte[] DataCD = { 0x23, 0x62, 0x10, 0x43, 0x44 };

        static readonly byte[] DataCdStartPlaying = { 0x38, 0x03, 0x00 };
        static readonly byte[] DataFM = { 0xA5, 0x62, 0x01, 0x41, 0x20, 0x20, 0x46, 0x4D, 0x20 };
        static readonly byte[] DataAM = { 0xA5, 0x62, 0x01, 0x41, 0x20, 0x20, 0x41, 0x4D, 0x20 };

        #endregion

        public BordmonitorAUX(IAudioPlayer player)
            : base(player)
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
        }

        #region AUX

        void ProcessRadioMessage(Message m)
        {
            if (m.Data.Compare(Bordmonitor.DataRadioOn))
            {
                IsRadioActive = true;
                m.ReceiverDescription = "Radio On";
            }
            else if (m.Data.Compare(Bordmonitor.DataRadioOff))
            {
                IsRadioActive = false;
                m.ReceiverDescription = "Radio Off";
            }
            else if (m.Data.Compare(Bordmonitor.DataAUX))
            {
                IsAUXSelected = true;
            }
            else if (m.Data.Compare(DataFM) || m.Data.Compare(DataAM) || m.Data.StartsWith(DataCdStartPlaying))
            {
                IsAUXSelected = false;
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

            base.OnIsEnabledChanged(isEnabled, fire);

            if (!isEnabled)
            {
                Player.PlayerHostState = IsRadioActive ? PlayerHostState.StandBy : PlayerHostState.Off;
                Pause();
            }
        }

        public bool IsRadioActive
        {
            get { return _isRadioActive; }
            set
            {
                if (_isRadioActive == value)
                {
                    return;
                }
                _isRadioActive = value;
                CheckAuxOn();
            }
        }

        public bool IsAUXSelected
        {
            get { return _isAUXSelected; }
            set
            {
                if (_isAUXSelected == value)
                {
                    return;
                }
                _isAUXSelected = value;
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
