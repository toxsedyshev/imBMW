using System;
using Microsoft.SPOT;
using imBMW.Multimedia;
using imBMW.iBus.Devices.Real;
using System.Threading;
using imBMW.Tools;
using imBMW.Features.Menu.Screens;
using imBMW.Features.Menu;
using imBMW.Features.Localizations;

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
        static byte[] DataFM = new byte[] { 0xA5, 0x62, 0x01, 0x41, 0x20, 0x20, 0x46, 0x4D, 0x20 };
        static byte[] DataAM = new byte[] { 0xA5, 0x62, 0x01, 0x41, 0x20, 0x20, 0x41, 0x4D, 0x20 };

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
            else if (m.Data.Compare(DataFM) || m.Data.Compare(DataAM) || m.Data.StartsWith(DataCDStartPlaying))
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
