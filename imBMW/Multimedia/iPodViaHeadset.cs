using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System;
using System.Threading;

namespace imBMW.Multimedia
{
    class iPodViaHeadset : IAudioPlayer, IDisposable
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

        OutputPort iPod;
        OutputPort iPodVolumeUp;
        OutputPort iPodVolumeDown;
        QueueThreadWorker iPodCommands;

        bool canControlVolume;
        bool isCurrentCDCPlayer;
        bool isCDCActive;
        bool isPlaying;
        bool isInVoiceOverMenu;
        DateTime voiceOverMenuStarted;

        public iPodViaHeadset(Cpu.Pin headsetControl, Cpu.Pin volumeUp = Cpu.Pin.GPIO_NONE, Cpu.Pin volumeDown = Cpu.Pin.GPIO_NONE)
        {
            iPod = new OutputPort(headsetControl, false);
            canControlVolume = volumeUp != Cpu.Pin.GPIO_NONE && volumeDown != Cpu.Pin.GPIO_NONE;
            if (canControlVolume)
            {
                iPodVolumeUp = new OutputPort(volumeUp, false);
                iPodVolumeDown = new OutputPort(volumeDown, false);
            }

            iPodCommands = new QueueThreadWorker(ExecuteIPodCommand);
        }

        void DisposePorts()
        {
            iPod.Dispose();
            if (canControlVolume)
            {
                iPodVolumeUp.Dispose();
                iPodVolumeDown.Dispose();
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
            iPod.Write(true);
            Thread.Sleep(milliseconds);
            iPod.Write(false);
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
                    Radio.DisplayText(((char)0xBC) + "" + ((char)0xBC) + " iPod   ", TextAlign.Center);
                    PressIPodButton();
                    PressIPodButton(true);
                    break;

                case iPodCommand.Prev:
                    Radio.DisplayText(((char)0xBD) + "" + ((char)0xBD) + " iPod   ", TextAlign.Center);
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
                            Radio.DisplayText(((char)0xBC) + " VoiceOver", TextAlign.Center);
                        }
                        else
                        {
                            IsPlaying = true; // Playing starts on VO select when paused
                        }
                    }
                    else
                    {
                        Radio.DisplayText(((char)0xC9) + " VoiceOver", TextAlign.Center);
                        PressIPodButton(false, 550); // Say current track
                    }
                    break;

                case iPodCommand.VoiceOverMenu:
                    IsInVoiceOverMenu = true;
                    break;

                case iPodCommand.VolumeUp:
                    iPodVolumeUp.Write(true);
                    Thread.Sleep(50);
                    iPodVolumeUp.Write(false);
                    Thread.Sleep(25);
                    break;

                case iPodCommand.VolumeDown:
                    iPodVolumeDown.Write(true);
                    Thread.Sleep(50);
                    iPodVolumeDown.Write(false);
                    Thread.Sleep(25);
                    break;
            }
            Debug.Print("iPod command: " + command.ToStringValue());
        }

        void EnqueueIPodCommand(iPodCommand command)
        {
            iPodCommands.Enqueue(command);
        }

        #endregion

        #region IAudioPlayer members

        public void Play()
        {
            EnqueueIPodCommand(iPodCommand.Play);
        }

        public void Pause()
        {
            EnqueueIPodCommand(iPodCommand.Pause);
        }

        public void PlayPauseToggle()
        {
            EnqueueIPodCommand(iPodCommand.PlayPauseToggle);
        }

        public void Next()
        {
            EnqueueIPodCommand(iPodCommand.Next);
        }

        public void Prev()
        {
            EnqueueIPodCommand(iPodCommand.Prev);
        }

        public void MFLRT()
        {
            PlayPauseToggle();
        }

        public void MFLDial()
        {
            VoiceOverCurrent();
        }

        public void MFLDialLong()
        {
            VoiceOverMenu();
        }

        public bool RandomToggle()
        {
            // Fixing IsPlaying flag, when playing iPod was connected to paused CDC
            isPlaying = !isPlaying;
            Radio.DisplayText(((char)(isPlaying ? 0xBC : 0xBE)) + " iPod  ", TextAlign.Center);
            return false; // Real status of iPod's shuffle-mode is unknown
        }

        public void VolumeUp()
        {
            if (canControlVolume)
            {
                EnqueueIPodCommand(iPodCommand.VolumeUp);
            }
        }

        public void VolumeDown()
        {
            if (canControlVolume)
            {
                EnqueueIPodCommand(iPodCommand.VolumeDown);
            }
        }

        public bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
            private set
            {
                if (isPlaying == value)
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
                isPlaying = value;
                IsInVoiceOverMenu = false;
                if (IsCDCActive)
                {
                    Radio.DisplayText(((char)(isPlaying ? 0xBC : 0xBE)) + " iPod  ", TextAlign.Center);
                }
            }
        }

        public bool IsCDCActive
        {
            get
            {
                return isCDCActive;
            }
            set
            {
                isCDCActive = value;
            }
        }

        public bool IsCurrentCDCPlayer
        {
            get
            {
                return isCurrentCDCPlayer;
            }
            set
            {
                isCurrentCDCPlayer = value;
                if (!isCurrentCDCPlayer)
                {
                    Pause();
                }
            }
        }

        #endregion

        public bool IsInVoiceOverMenu
        {
            get
            {
                if (isInVoiceOverMenu && (DateTime.Now - voiceOverMenuStarted).GetTotalSeconds() >= VoiceOverMenuTimeoutSeconds)
                {
                    IsInVoiceOverMenu = false;
                }
                return isInVoiceOverMenu;
            }
            private set
            {
                if (value)
                {
                    Radio.DisplayText(((char)0xC8) + " VoiceOver", TextAlign.Center);
                    voiceOverMenuStarted = DateTime.Now;
                    PressIPodButton(false, 5000);
                }
                isInVoiceOverMenu = value;
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
