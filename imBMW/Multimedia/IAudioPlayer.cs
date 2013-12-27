using System;
using Microsoft.SPOT;

namespace imBMW.Multimedia
{
    public interface IAudioPlayer
    {
        void Next();

        void Prev();

        void Play();

        void Pause();

        void PlayPauseToggle();

        void MFLRT();

        void MFLDial();

        void MFLDialLong();

        bool RandomToggle();

        void VolumeUp();

        void VolumeDown();

        bool IsPlaying { get; }

        bool IsCurrentPlayer { get; set; }

        bool IsPlayerHostActive { get; set; }

        string ShortName { get; }
    }
}
