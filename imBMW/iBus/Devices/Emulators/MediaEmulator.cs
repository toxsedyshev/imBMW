using System;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;

namespace imBMW.iBus.Devices.Emulators
{
    public delegate void MediaEmulatorEnabledEventHandler(MediaEmulator emulator, bool isEnabled);

    public delegate void PlayerChangedHandler(IAudioPlayer sender);

    public abstract class MediaEmulator
    {
        private bool _isEnabled;
        private IAudioPlayer _player;

        protected MediaEmulator(IAudioPlayer player)
        {
            Player = player;

            MultiFunctionSteeringWheel.ButtonPressed += MultiFunctionSteeringWheel_ButtonPressed;
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            protected set
            {
                if (_isEnabled == value)
                {
                    return;
                }
                _isEnabled = value;
                OnIsEnabledChanged(value);
            }
        }

        public event MediaEmulatorEnabledEventHandler IsEnabledChanged;

        protected virtual void OnIsEnabledChanged(bool isEnabled, bool fire = true)
        {
            if (fire)
            {
                FireIsEnabledChanged(isEnabled);
            }
        }

        protected void FireIsEnabledChanged(bool isEnabled)
        {
            var e = IsEnabledChanged;
            if (e != null)
            {
                e(this, isEnabled);
            }
        }

        protected void FireIsEnabledChanged()
        {
            FireIsEnabledChanged(IsEnabled);
        }

        public IAudioPlayer Player
        {
            get
            {
                return _player;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (_player != null)
                {
                    UnsetPlayer(_player);
                }
                _player = value;
                SetupPlayer(value);
            }
        }

        public event IsPlayingHandler PlayerIsPlayingChanged;

        public event PlayerStatusHandler PlayerStatusChanged;

        public event PlayerChangedHandler PlayerChanged;

        void SetupPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = true;
            player.IsPlayingChanged += player_IsPlayingChanged;
            player.StatusChanged += player_StatusChanged;

            var e = PlayerChanged;
            if (e != null)
            {
                e(player);
            }
        }

        void UnsetPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = false;
            player.IsPlayingChanged -= player_IsPlayingChanged;
            player.StatusChanged -= player_StatusChanged;
        }

        void player_StatusChanged(IAudioPlayer sender, string status, PlayerEvent playerEvent)
        {
            var e = PlayerStatusChanged;
            if (e != null)
            {
                e(sender, status, playerEvent);
            }
        }

        void player_IsPlayingChanged(IAudioPlayer sender, bool isPlaying)
        {
            var e = PlayerIsPlayingChanged;
            if (e != null)
            {
                e(sender, isPlaying);
            }
        }

        void MultiFunctionSteeringWheel_ButtonPressed(MFLButton button)
        {
            if (!IsEnabled)
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

        protected virtual void Play()
        {
            Player.Play();
        }

        protected virtual void Pause()
        {
            Player.Pause();
        }

        protected virtual void PlayPauseToggle()
        {
            Player.PlayPauseToggle();
        }

        protected virtual void Next()
        {
            Player.Next();
        }

        protected virtual void Prev()
        {
            Player.Prev();
        }

        protected virtual void MFLRT()
        {
            //Player.MFLRT();
        }

        protected virtual void MFLDial()
        {
            Player.MFLDial();
        }

        protected virtual void MFLDialLong()
        {
            Player.MFLDialLong();
        }

        protected virtual void RandomToggle()
        {
            bool rnd = Player.RandomToggle();
            // TODO send rnd status to radio
        }

    }
}
