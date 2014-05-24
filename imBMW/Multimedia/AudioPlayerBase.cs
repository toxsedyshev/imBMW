using System;
using imBMW.Features.Menu;

namespace imBMW.Multimedia
{
    public abstract class AudioPlayerBase : IAudioPlayer
    {
        bool _isCurrentPlayer;
        PlayerHostState _playerHostState;
        bool _isEnabled;

        protected bool _isPlaying;

        public virtual void Play()
        {
            SetPlaying(true);
        }

        public virtual void Pause()
        {
            SetPlaying(false);
        }

        public virtual void PlayPauseToggle()
        {
            SetPlaying(!IsPlaying);
        }

        protected virtual void SetPlaying(bool value)
        {
            IsPlaying = value;
        }

        public abstract void Next();

        public abstract void Prev();

        public abstract void MFLRT();

        public abstract void MFLDial();

        public abstract void MFLDialLong();

        public abstract bool RandomToggle();

        public abstract void VolumeUp();

        public abstract void VolumeDown();

        public abstract MenuScreen Menu { get; }

        public string Name { get; protected set; }

        public abstract bool IsPlaying
        {
            get;
            protected set;
        }

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            private set
            {
                if (_isEnabled == value)
                {
                    return;
                }
                _isEnabled = value;
                OnIsEnabledChanged(value);
            }
        }

        public PlayerHostState PlayerHostState
        {
            get
            {
                return _playerHostState;
            }
            set
            {
                if (_playerHostState == value)
                {
                    return;
                }
                _playerHostState = value;
                OnPlayerHostStateChanged(value);
            }
        }

        public bool IsCurrentPlayer
        {
            get
            {
                return _isCurrentPlayer;
            }
            set
            {
                if (_isCurrentPlayer == value)
                {
                    return;
                }
                _isCurrentPlayer = value;
                OnIsCurrentPlayerChanged(value);
            }
        }

        protected virtual void OnIsEnabledChanged(bool isEnabled)
        {
        }

        protected virtual void OnIsCurrentPlayerChanged(bool isCurrentPlayer)
        {
            CheckIsEnabled();
            if (!isCurrentPlayer)
            {
                Pause();
            }
            else if (PlayerHostState == PlayerHostState.On)
            {
                Play();
            }
        }

        protected virtual void OnPlayerHostStateChanged(PlayerHostState playerHostState)
        {
            CheckIsEnabled();
        }

        void CheckIsEnabled()
        {
            IsEnabled = PlayerHostState == PlayerHostState.On && IsCurrentPlayer;
        }

        public event IsPlayingHandler IsPlayingChanged;

        public event PlayerStatusHandler StatusChanged;

        protected virtual void OnIsPlayingChanged(bool isPlaying)
        {
            var e = IsPlayingChanged;
            if (e != null)
            {
                e.Invoke(this, isPlaying);
            }
        }

        protected virtual void OnStatusChanged(PlayerEvent playerEvent)
        {
            OnStatusChanged(String.Empty, playerEvent);
        }

        protected virtual void OnStatusChanged(string status, PlayerEvent playerEvent)
        {
            var e = StatusChanged;
            if (e != null)
            {
                e.Invoke(this, status, playerEvent);
            }
        }
    }
}
