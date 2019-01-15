using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Features.Menu;
using imBMW.Multimedia.Models;
using imBMW.Tools;

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

        public bool IsShowStatusOnIKEEnabled { get; set; }

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
            var args = new AudioPlayerIsPlayingStatusEventArgs { IsPlaying = isPlaying };
            var e = IsPlayingChanged;
            if (e != null)
            {
                e.Invoke(this, args);
            }

            if (IsShowStatusOnIKEEnabled && !args.IsShownOnIKE)
            {
                ShowIKEStatusPlaying(isPlaying);
            }
        }

        protected virtual void OnStatusChanged(PlayerEvent playerEvent)
        {
            OnStatusChanged(String.Empty, playerEvent);
        }

        protected virtual void OnStatusChanged(string status, PlayerEvent playerEvent)
        {
            var args = new AudioPlayerStatusEventArgs
            {
                Status = status,
                Event = playerEvent
            };
            var e = StatusChanged;
            if (e != null)
            {
                e.Invoke(this, args);
            }

            if (IsShowStatusOnIKEEnabled && !args.IsShownOnIKE)
            {
                ShowIKEStatus(status, playerEvent);
            }
        }

        protected virtual void OnNowPlayingChanged(TrackInfo nowPlaying)
        {
            var e = NowPlayingChanged;
            if (e != null)
            {
                e.Invoke(this, nowPlaying);
            }

            if (IsShowStatusOnIKEEnabled)
            {
                ShowIKEStatus(nowPlaying);
            }
        }

        private void ShowIKEStatusPlaying(bool isPlaying)
        {
            ShowIKEStatus(GetPlayingStatusString(isPlaying, InstrumentClusterElectronics.DisplayTextMaxLength));
        }

        private void ShowIKEStatus(string status, PlayerEvent playerEvent)
        {
            if (StringHelpers.IsNullOrEmpty(status))
            {
                status = Name;
            }
            status = GetStatusString(status, playerEvent, InstrumentClusterElectronics.DisplayTextMaxLength);
            ShowIKEStatus(status);
        }

        private void ShowIKEStatus(TrackInfo nowPlaying)
        {
            ShowIKEStatus(nowPlaying.Title.TextWithIcon(CharIcons.Play, InstrumentClusterElectronics.DisplayTextMaxLength));
        }

        private void ShowIKEStatus(string s)
        {
            InstrumentClusterElectronics.DisplayTextWithDelay(s, TextAlign.Left);
        }

        public string GetStatusString(string status, PlayerEvent playerEvent, int maxLength)
        {
            if (StringHelpers.IsNullOrEmpty(status))
            {
                status = Name;
            }
            switch (playerEvent)
            {
                case PlayerEvent.Next:
                    status = status.TextWithIcon(CharIcons.Next, maxLength);
                    break;
                case PlayerEvent.Prev:
                    status = status.TextWithIcon(CharIcons.Prev, maxLength);
                    break;
                case PlayerEvent.Playing:
                    status = status.TextWithIcon(CharIcons.Play, maxLength);
                    break;
                case PlayerEvent.Current:
                    status = status.TextWithIcon(CharIcons.SelectedArrow, maxLength);
                    break;
                case PlayerEvent.Voice:
                case PlayerEvent.Settings:
                    status = status.TextWithIcon(CharIcons.Voice, maxLength);
                    break;
            }
            return status;
        }

        public string GetPlayingStatusString(bool isPlaying, int maxLength)
        {
            return Name.TextWithIcon(isPlaying ? CharIcons.Play : CharIcons.Pause, maxLength);
        }
    }
}
