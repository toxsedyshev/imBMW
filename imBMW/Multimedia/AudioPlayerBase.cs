using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;

namespace imBMW.Multimedia
{
    public abstract class AudioPlayerBase : IAudioPlayer
    {
        protected bool isCurrentPlayer;
        protected bool isPlayerHostActive;
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

        public abstract void MFLRT();

        public abstract void MFLDial();

        public abstract void MFLDialLong();

        public abstract bool RandomToggle();

        public abstract void VolumeUp();

        public abstract void VolumeDown();

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
                return IsPlayerHostActive && IsCurrentPlayer;
            }
        }

        public bool IsPlayerHostActive
        {
            get
            {
                return isPlayerHostActive;
            }
            set
            {
                isPlayerHostActive = value;
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
                isCurrentPlayer = value;
                if (!isCurrentPlayer)
                {
                    Pause();
                }
                else if (IsPlayerHostActive)
                {
                    Play();
                }
            }
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
