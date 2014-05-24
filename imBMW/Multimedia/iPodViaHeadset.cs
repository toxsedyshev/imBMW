using imBMW.Features.Menu;
using imBMW.Tools;
using Microsoft.SPOT.Hardware;
using System;
using System.Threading;

namespace imBMW.Multimedia
{
    public class iPodViaHeadset : AudioPlayerBase, IDisposable
    {
        public enum iPodCommand
        {
            Play,
            Pause,
            PlayPauseToggle,
            Next,
            Prev,
            VoiceOverCurrent,
            VoiceOverMenu,
            VolumeUp,
            VolumeDown
        }

        const int VoiceOverMenuTimeoutSeconds = 60;

        readonly OutputPort _iPod;
        readonly OutputPort _iPodVolumeUp;
        readonly OutputPort _iPodVolumeDown;
        readonly QueueThreadWorker _iPodCommands;

        readonly bool _canControlVolume;
        bool _isInVoiceOverMenu;
        DateTime _voiceOverMenuStarted;

        public iPodViaHeadset(Cpu.Pin headsetControl, Cpu.Pin volumeUp = Cpu.Pin.GPIO_NONE, Cpu.Pin volumeDown = Cpu.Pin.GPIO_NONE)
        {
            Name = "iPod";
            _iPod = new OutputPort(headsetControl, false);
            _canControlVolume = volumeUp != Cpu.Pin.GPIO_NONE && volumeDown != Cpu.Pin.GPIO_NONE;
            if (_canControlVolume)
            {
                _iPodVolumeUp = new OutputPort(volumeUp, false);
                _iPodVolumeDown = new OutputPort(volumeDown, false);
            }

            _iPodCommands = new QueueThreadWorker(ExecuteIPodCommand);
        }

        void DisposePorts()
        {
            _iPod.Dispose();
            if (_canControlVolume)
            {
                _iPodVolumeUp.Dispose();
                _iPodVolumeDown.Dispose();
            }
        }

        public void Dispose()
        {
            DisposePorts();
            GC.SuppressFinalize(this);
        }

        ~iPodViaHeadset()
        {
            DisposePorts();
        }

        #region Headset logic

        void PressIPodButton(bool longPause = false, int milliseconds = 50)
        {
            _iPod.Write(true);
            Thread.Sleep(milliseconds);
            _iPod.Write(false);
            Thread.Sleep(longPause ? 300 : 25); // Let iPod understand the command
        }

        void ExecuteIPodCommand(object c)
        {
            var command = (iPodCommand)c;
            switch (command)
            {
                case iPodCommand.PlayPauseToggle:
                    IsPlaying = !IsPlaying;
                    break;

                case iPodCommand.Play:
                    IsPlaying = true;
                    break;

                case iPodCommand.Pause:
                    IsPlaying = false;
                    break;

                case iPodCommand.Next:
                    OnStatusChanged(PlayerEvent.Next);
                    PressIPodButton();
                    PressIPodButton(true);
                    break;

                case iPodCommand.Prev:
                    OnStatusChanged(PlayerEvent.Prev);
                    PressIPodButton();
                    PressIPodButton();
                    PressIPodButton(true);
                    break;

                case iPodCommand.VoiceOverCurrent:
                    if (IsInVoiceOverMenu)
                    {
                        if (IsPlaying)
                        {
                            PressIPodButton(true); // Select currently saying playlist
                            IsInVoiceOverMenu = false;
                            OnStatusChanged("VoiceOver", PlayerEvent.Playing);
                        }
                        else
                        {
                            IsPlaying = true; // Playing starts on VO select when paused
                        }
                    }
                    else
                    {
                        OnStatusChanged("VoiceOver", PlayerEvent.Voice);
                        PressIPodButton(false, 550); // Say current track
                    }
                    break;

                case iPodCommand.VoiceOverMenu:
                    IsInVoiceOverMenu = true;
                    break;

                case iPodCommand.VolumeUp:
                    _iPodVolumeUp.Write(true);
                    Thread.Sleep(50);
                    _iPodVolumeUp.Write(false);
                    Thread.Sleep(25);
                    break;

                case iPodCommand.VolumeDown:
                    _iPodVolumeDown.Write(true);
                    Thread.Sleep(50);
                    _iPodVolumeDown.Write(false);
                    Thread.Sleep(25);
                    break;
            }
            Logger.Info("iPod command: " + command.ToStringValue());
        }

        void EnqueueIPodCommand(iPodCommand command)
        {
            _iPodCommands.Enqueue(command);
        }

        #endregion

        #region IAudioPlayer members

        public override void Play()
        {
            EnqueueIPodCommand(iPodCommand.Play);
        }

        public override void Pause()
        {
            EnqueueIPodCommand(iPodCommand.Pause);
        }

        public override void PlayPauseToggle()
        {
            EnqueueIPodCommand(iPodCommand.PlayPauseToggle);
        }

        public override void Next()
        {
            EnqueueIPodCommand(iPodCommand.Next);
        }

        public override void Prev()
        {
            EnqueueIPodCommand(iPodCommand.Prev);
        }

        public override void MFLRT()
        {
            PlayPauseToggle();
        }

        public override void MFLDial()
        {
            VoiceOverCurrent();
        }

        public override void MFLDialLong()
        {
            VoiceOverMenu();
        }

        public override bool RandomToggle()
        {
            // Fixing IsPlaying flag, when playing iPod was connected to paused CDC
            _isPlaying = !_isPlaying;
            OnIsPlayingChanged(IsPlaying);
            return false; // Real status of iPod's shuffle-mode is unknown
        }

        public override void VolumeUp()
        {
            if (_canControlVolume)
            {
                EnqueueIPodCommand(iPodCommand.VolumeUp);
            }
        }

        public override void VolumeDown()
        {
            if (_canControlVolume)
            {
                EnqueueIPodCommand(iPodCommand.VolumeDown);
            }
        }

        public override bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            protected set
            {
                if (_isPlaying == value)
                {
                    return;
                }
                if (IsInVoiceOverMenu)
                {
                    /**
                     * Trying to prevent the next situation:
                     * 1. VO menu started
                     * 2. PlayPause pressed
                     * 3. Playlist selected instead of PlayPause
                     * 4. IsPlaying flag does't match the iPod playing status
                     */
                    IsInVoiceOverMenu = false;
                    return;
                }
                PressIPodButton(true);
                _isPlaying = value;
                IsInVoiceOverMenu = false;
                OnIsPlayingChanged(value);
            }
        }

        public override MenuScreen Menu
        {
            get
            {
                return null; // TODO
            }
        }

        #endregion

        public bool IsInVoiceOverMenu
        {
            get
            {
                if (_isInVoiceOverMenu && (DateTime.Now - _voiceOverMenuStarted).GetTotalSeconds() >= VoiceOverMenuTimeoutSeconds)
                {
                    IsInVoiceOverMenu = false;
                }
                return _isInVoiceOverMenu;
            }
            private set
            {
                if (value)
                {
                    OnStatusChanged("VoiceOver", PlayerEvent.Current);
                    _voiceOverMenuStarted = DateTime.Now;
                    PressIPodButton(false, 5000);
                }
                _isInVoiceOverMenu = value;
            }
        }

        public void VoiceOverCurrent()
        {
            EnqueueIPodCommand(iPodCommand.VoiceOverCurrent);
        }

        public void VoiceOverMenu()
        {
            EnqueueIPodCommand(iPodCommand.VoiceOverMenu);
        }

    }
}
