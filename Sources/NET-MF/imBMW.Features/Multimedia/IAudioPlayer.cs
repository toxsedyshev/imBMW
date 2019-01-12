using System;
using Microsoft.SPOT;
using imBMW.Features.Menu;
using imBMW.Multimedia.Models;

namespace imBMW.Multimedia
{
    public delegate void IsPlayingHandler(IAudioPlayer sender, AudioPlayerIsPlayingStatusEventArgs args);

    public delegate void PlayerStatusHandler(IAudioPlayer sender, AudioPlayerStatusEventArgs args);

    public delegate void NowPlayingHandler(IAudioPlayer sender, TrackInfo nowPlaying);

    public class AudioPlayerStatusEventArgs
    {
        public string Status { get; set; }

        public PlayerEvent Event { get; set; }

        public bool IsShownOnIKE { get; set; }
    }

    public class AudioPlayerIsPlayingStatusEventArgs
    {
        public bool IsPlaying { get; set; }

        public bool IsShownOnIKE { get; set; }
    }

    public enum PlayerEvent
    {
        Next,
        Prev,
        Voice,
        Current,
        Playing,
        Wireless,
        IncomingCall,
        Text,
        Settings
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

        void VoiceButtonPress();

        void VoiceButtonLongPress();

        bool RandomToggle();

        void VolumeUp();

        void VolumeDown();

        bool IsPlaying { get; }

        bool IsEnabled { get; }

        bool IsCurrentPlayer { get; set; }

        PlayerHostState PlayerHostState { get; set; }

        string Name { get; }

        bool IsShowStatusOnIKEEnabled { get; set; }

        MenuScreen Menu { get; }

        event IsPlayingHandler IsPlayingChanged;

        event PlayerStatusHandler StatusChanged;

        string GetStatusString(string status, PlayerEvent playerEvent, int statusTextMaxlen);

        string GetPlayingStatusString(bool isPlaying, int statusTextMaxlen);
    }
}
