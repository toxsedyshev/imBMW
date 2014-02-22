using System;
using Microsoft.SPOT;
using imBMW.Multimedia;
using imBMW.iBus.Devices.Real;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Emulators
{
    public static class BordmonitorAUX
    {
        static IAudioPlayer player;
        static bool isRadioActive = false;
        static bool isAUXSelected = false;

        #region Messages

        static Message MessageAUXHighVolumeP6 = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Set AUX high volume", 0x48, 0x03);

        static byte[] DataRadioOn = new byte[] { 0x4A, 0xFF }; // Some cassette-control messages
        static byte[] DataRadioOff = new byte[] { 0x4A, 0x00 }; // or 4A 90?
        static byte[] DataAUX = new byte[] { 0x23, 0x62, 0x10, 0x41, 0x55, 0x58, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        static byte[] DataCD = new byte[] { 0x23, 0x62, 0x10, 0x43, 0x44 };
        static byte[] DataCDStartPlaying = new byte[] { 0x38, 0x03, 0x00 };
        static byte[] DataFM = new byte[] { 0xA5, 0x62, 0x01, 0x41, 0x20, 0x20, 0x46, 0x4D, 0x20 };
        static byte[] DataAM = new byte[] { 0xA5, 0x62, 0x01, 0x41, 0x20, 0x20, 0x41, 0x4D, 0x20 };

        #endregion

        public static void Init(IAudioPlayer player)
        {
            Player = player;

            // TODO remove
            //IsAUXSelected = true;
            //IsRadioActive = true;

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
            MultiFunctionSteeringWheel.ButtonPressed += MultiFunctionSteeringWheel_ButtonPressed;
        }

        #region AUX

        static void ProcessRadioMessage(Message m)
        {
            if (m.Data.Compare(DataRadioOn))
            {
                IsRadioActive = true;
                m.ReceiverDescription = "Radio On";
            }
            else if (m.Data.Compare(DataRadioOff))
            {
                IsRadioActive = false;
                m.ReceiverDescription = "Radio Off";
            }
            else if (m.Data.Compare(DataAUX))
            {
                IsAUXSelected = true;
                if (lastStatus == null)
                {
                    ShowPlayerStatus(player);
                }
                else
                {
                    ShowPlayerStatus(player, lastStatus);
                    lastStatus = null;
                }
            }
            else if (m.Data.Compare(DataFM) || m.Data.Compare(DataAM) || m.Data.Compare(true, DataCDStartPlaying))
            {
                IsAUXSelected = false;
            }
        }

        static void CheckAuxOn()
        {
            if (IsAUXOn)
            {
                player.IsPlayerHostActive = true;
                Play();
            }
            else
            {
                player.IsPlayerHostActive = false;
                Pause();
            }
        }

        public static bool IsRadioActive
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

        public static bool IsAUXSelected
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

        public static bool IsAUXOn
        {
            get
            {
                return IsAUXSelected && IsRadioActive;
            }
        }

        #endregion

        #region Player control

        internal static IAudioPlayer Player
        {
            get
            {
                return player;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (player != null)
                {
                    UnsetPlayer(player);
                }
                player = value;
                SetupPlayer(value);
            }
        }

        static void SetupPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = true;
            player.IsPlayingChanged += ShowPlayerStatus;
            player.StatusChanged += ShowPlayerStatus;

            ShowPlayerName();
        }

        static void ShowPlayerName()
        {
            if (!IsAUXOn)
            {
                return;
            }
            Bordmonitor.ShowText(player.Name, BordmonitorFields.Title);
        }

        static void UnsetPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = false;
            player.IsPlayingChanged -= ShowPlayerStatus;
            player.StatusChanged -= ShowPlayerStatus;
        }

        static void ShowPlayerStatus(IAudioPlayer player)
        {
            ShowPlayerStatus(player, player.IsPlaying);
        }

        static void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        {
            string s = isPlaying ? "Играет" : "Пауза";
            ShowPlayerStatus(Player, s);
        }
        
        static Timer displayTextDelayTimer;
        const int displayTextDelay = 5000;
        const int displayTextMaxlen = 11;
        static string lastStatus;

        static void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent)
        {
            if (!IsAUXOn)
            {
                return;
            }
            bool showAfterWithDelay = false;
            switch (playerEvent)
            {
                case PlayerEvent.Next:
                    status = "Вперед";
                    showAfterWithDelay = true;
                    break;
                case PlayerEvent.Prev:
                    status = "Назад";
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
            lastStatus = status;
            ShowPlayerStatus(player, status);
            if (showAfterWithDelay)
            {
                ShowPlayerStatusWithDelay(player);
            }
        }

        static string TextWithIcon(string icon, string text = null)
        {
            if (text == null)
            {
                text = "";
            }
            if (icon.Length + text.Length < displayTextMaxlen)
            {
                return icon + " " + text;
            }
            else
            {
                return icon + text;
            }
        }

        static void ShowPlayerStatus(IAudioPlayer player, string status)
        {
            if (!IsAUXOn)
            {
                return;
            }
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }

            Bordmonitor.ShowText(status, BordmonitorFields.Status);
            ShowPlayerName();
            //ShowPlayerStatusWithDelay(player);
        }
        
        public static void ShowPlayerStatusWithDelay(IAudioPlayer player)
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

        static void MultiFunctionSteeringWheel_ButtonPressed(MFLButton button)
        {
            if (!IsAUXOn)
            {
                return;
            }
            switch (button)
            {
                case MFLButton.Next:
                    Next();
                    break;
                case MFLButton.Prev:
                    Prev();
                    break;
                case MFLButton.RT:
                    MFLRT();
                    break;
                case MFLButton.Dial:
                    MFLDial();
                    break;
                case MFLButton.DialLong:
                    MFLDialLong();
                    break;
            }
        }

        static void Play()
        {
            Player.Play();
        }

        static void Pause()
        {
            Player.Pause();
        }

        static void PlayPauseToggle()
        {
            Player.PlayPauseToggle();
        }

        static void Next()
        {
            Player.Next();
        }

        static void Prev()
        {
            Player.Prev();
        }

        static void MFLRT()
        {
            Player.MFLRT();
        }

        static void MFLDial()
        {
            Player.MFLDial();
        }

        static void MFLDialLong()
        {
            Player.MFLDialLong();
        }

        static void RandomToggle()
        {
            bool rnd = Player.RandomToggle();
            // TODO send rnd status to radio
        }

        #endregion

    }
}
