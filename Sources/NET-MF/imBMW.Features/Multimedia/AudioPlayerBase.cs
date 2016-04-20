using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Features.Menu;
using imBMW.Multimedia.Models;

namespace imBMW.Multimedia
{
    public abstract class AudioPlayerBase : IAudioPlayer
    {
        bool isCurrentPlayer;
        PlayerHostState playerHostState;
        bool isEnabled;
        TrackInfo nowPlaying;
        protected bool isPlaying;

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

        public abstract void VoiceButtonPress();

        public abstract void VoiceButtonLongPress();

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

        public TrackInfo NowPlaying
        {
            get
            {
                if (nowPlaying == null)
                {
                    nowPlaying = new TrackInfo();
                }
                return nowPlaying;
            }
            protected set
            {
                nowPlaying = value;
                OnNowPlayingChanged(value);
            }
        }

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            private set
            {
                if (isEnabled == value)
                {
                    return;
                }
                isEnabled = value;
                OnIsEnabledChanged(value);
            }
        }

        public PlayerHostState PlayerHostState
        {
            get
            {
                return playerHostState;
            }
            set
            {
                if (playerHostState == value)
                {
                    return;
                }
                playerHostState = value;
                OnPlayerHostStateChanged(value);
            }
        }

        public bool IsCurrentPlayer
        {
            get
            {
                return isCurrentPlayer;
            }
            set
            {
                if (isCurrentPlayer == value)
                {
                    return;
                }
                isCurrentPlayer = value;
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
            else if (PlayerHostState == Multimedia.PlayerHostState.On)
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
            IsEnabled = PlayerHostState == Multimedia.PlayerHostState.On && IsCurrentPlayer;
        }

        public event IsPlayingHandler IsPlayingChanged;

        public event PlayerStatusHandler StatusChanged;

        public event NowPlayingHandler NowPlayingChanged;

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

        protected virtual void OnNowPlayingChanged(TrackInfo nowPlaying)
        {
            var e = NowPlayingChanged;
            if (e != null)
            {
                e.Invoke(this, nowPlaying);
            }
        }
    }
}
