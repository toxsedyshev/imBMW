using System;
using Microsoft.SPOT;
using imBMW.Features.Menu;

namespace imBMW.Multimedia
{
    public delegate void IsPlayingHandler(IAudioPlayer sender, bool isPlaying);

    public delegate void PlayerStatusHandler(IAudioPlayer sender, string status, PlayerEvent playerEvent);

    public enum PlayerEvent
    {
        Next,
        Prev,
        Voice,
        Current,
        Playing,
        Wireless,
        IncomingCall,
        Text
    }

    public enum PlayerHostState
    {
        Off,
        StandBy,
        On
    }

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

        bool IsEnabled { get; }

        bool IsCurrentPlayer { get; set; }

        PlayerHostState PlayerHostState { get; set; }

        string Name { get; }

        MenuScreen Menu { get; }

        event IsPlayingHandler IsPlayingChanged;

        event PlayerStatusHandler StatusChanged;
    }
}
