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

        static byte[] DataRadioAlive = new byte[] { 0x02, 0x00, 0x03 };
        static byte[] DataRadioOn = new byte[] { 0x4A, 0xFF }; // Some cassette-control messages
        static byte[] DataRadioOff = new byte[] { 0x4A, 0x00 }; // or 4A 90?

        #endregion

        public static void Init(IAudioPlayer player)
        {
            Player = player;

            // TODO remove
            IsAUXSelected = true;
            //IsRadioActive = true;

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
            MultiFunctionSteeringWheel.ButtonPressed += MultiFunctionSteeringWheel_ButtonPressed;
        }

        #region AUX

        static void ProcessRadioMessage(Message m)
        {
            if (m.Data.Compare(DataRadioOn) || m.Data.Compare(DataRadioAlive))
            {
                IsRadioActive = true;
                m.ReceiverDescription = "Radio On";
            }
            else if (m.Data.Compare(DataRadioOff))
            {
                IsRadioActive = false;
                m.ReceiverDescription = "Radio Off";
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
            Bordmonitor.ShowText(player.Name, BordmonitorFields.Title);
        }

        static void UnsetPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = false;
            player.IsPlayingChanged -= ShowPlayerStatus;
            player.StatusChanged -= ShowPlayerStatus;
        }

        static void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        {
            string s = isPlaying ? "Играет" : "Пауза";
            ShowPlayerStatus(Player, s);
        }
        
        static Timer displayTextDelayTimer;
        const int displayTextDelay = 5000;
        const int displayTextMaxlen = 11;

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
                ShowPlayerStatus(player, player.IsPlaying);
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
