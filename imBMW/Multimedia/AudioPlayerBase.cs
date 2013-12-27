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
            IsPlaying = true;
        }

        public virtual void Pause()
        {
            IsPlaying = false;
        }

        public virtual void PlayPauseToggle()
        {
            IsPlaying = !IsPlaying;
        }

        public abstract void Next();

        public abstract void Prev();

        public abstract void MFLRT();

        public abstract void MFLDial();

        public abstract void MFLDialLong();

        public abstract bool RandomToggle();

        public abstract void VolumeUp();

        public abstract void VolumeDown();

        public string ShortName { get; protected set; }

        public abstract bool IsPlaying
        {
            get;
            protected set;
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
                if (isPlayerHostActive && IsPlaying)
                {
                    ShowCurrentStatus(true);
                }
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
            }
        }

        protected virtual void ShowCurrentStatus(bool delay = false)
        {
            string s = ((char)(isPlaying ? 0xBC : 0xBE)) + " " + ShortName + "  ";
            if (delay)
            {
                Radio.DisplayTextWithDelay(s, TextAlign.Center);
            }
            else
            {
                Radio.DisplayText(s, TextAlign.Center);
            }
        }
    }
}
