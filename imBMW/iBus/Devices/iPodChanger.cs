using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus.Devices
{
    static class iPodChanger
    {
        const int VoiceOverMenuTimeoutSeconds = 60;
        const int StopDelayMilliseconds = 1000;

        static OutputPort iPod;
        static Thread announceThread;
        static QueueThreadWorker iPodCommands;
        static Timer stopDelay;

        static bool isPlaying;
        static bool isInVoiceOverMenu;
        static bool wasDialLongPressed;
        static bool isCDCActive;
        static bool needSkipRT;
        static bool isIgnitionOn;
        static DateTime voiceOverMenuStarted;

        #region Messages

        static Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        static Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x01);
        static Message MessagePhoneResponse = new Message(DeviceAddress.Telephone, DeviceAddress.Broadcast, 0x02, 0x00);
        static Message MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, 0x39, 0x00, 0x09, 0x00, 0x3F, 0x00, 0x01, 0x01);

        static byte[] DataPollRequest = new byte[] { 0x01 };
        static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };
        static byte[] DataStopPlaying  = new byte[] { 0x38, 0x01, 0x00 };
        static byte[] DataStartPlaying = new byte[] { 0x38, 0x03, 0x00 };
        static byte[] DataRandomPlay   = new byte[] { 0x38, 0x08, 0x01 };

        static byte[] DataNextPressed = new byte[] { 0x3B, 0x01 };
        static byte[] DataPrevPressed = new byte[] { 0x3B, 0x08 };
        static byte[] DataRTPressedR  = new byte[] { 0x3B, 0x40 };
        static byte[] DataRTPressedT  = new byte[] { 0x3B, 0x00 };
        static byte[] DataDialPressed = new byte[] { 0x3B, 0x80 };
        static byte[] DataDialLongPressed = new byte[] { 0x3B, 0x90 };
        static byte[] DataDialReleased = new byte[] { 0x3B, 0xA0 };

        static byte[] DataIgnitionOff = new byte[] { 0x11, 0x00 };
        static byte[] DataIgnitionOn  = new byte[] { 0x11, 0x01 };

        #endregion

        public static void Init(Cpu.Pin headsetControl)
        {
            iPod = new OutputPort(headsetControl, false);

            iPodCommands = new QueueThreadWorker(ExecuteIPodCommand);

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.MultiFunctionSteeringWheel, ProcessMFLMessage);
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, ProcessIKEMessage);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, ProcessCDCMessage);

            announceThread = new Thread(announce);
            announceThread.Start();
        }

        #region iPod control

        public enum iPodCommand
        {
            Play,
            Pause,
            PlayPauseToggle,
            Next,
            Prev,
            VoiceOverCurrent,
            VoiceOverMenu
        }

        static void ProcessIKEMessage(Message m)
        {
            if (m.Data.Compare(DataIgnitionOn))
            {
                if (!isIgnitionOn)
                {
                    // MFL sends RT 00 signal on ignition 00 > 01
                    needSkipRT = true;
                }
                isIgnitionOn = true;
                Debug.Print("Ignition ON");
            }
            else if (m.Data.Compare(DataIgnitionOff))
            {
                isIgnitionOn = false;
                Debug.Print("Ignition OFF");
            }
        }

        static void ProcessMFLMessage(Message m)
        {
            if (!isCDCActive)
            {
                return;
            }
            if (m.DestinationDevice == DeviceAddress.Telephone && m.Data.Compare(DataPollRequest))
            {
                Manager.EnqueueMessage(MessagePhoneResponse);
            }
            else if (m.Data.Compare(DataNextPressed))
            {
                Next();
            }
            else if (m.Data.Compare(DataPrevPressed))
            {
                Prev();
            }
            else if (m.Data.Compare(DataRTPressedR) || m.Data.Compare(DataRTPressedT))
            {
                if (!needSkipRT || m.Data.Compare(DataRTPressedR))
                {
                    PlayPauseToggle();
                }
                needSkipRT = false;
            }
            else if (m.Data.Compare(DataDialPressed))
            {
                wasDialLongPressed = false;
            }
            else if (m.Data.Compare(DataDialLongPressed))
            {
                wasDialLongPressed = true;
                VoiceOverMenu();
            }
            else if (m.Data.Compare(DataDialReleased))
            {
                if (!wasDialLongPressed)
                {
                    VoiceOverCurrent();
                }
                wasDialLongPressed = false;
            }
            Debug.Print(m.PrettyDump);
        }

        static void PressIPodButton(bool longPause = false, int milliseconds = 50)
        {
            iPod.Write(true);
            Thread.Sleep(milliseconds);
            iPod.Write(false);
            Thread.Sleep(longPause ? 300 : 25); // Let iPod understand the command
        }

        static void ExecuteIPodCommand(object c)
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
                    CarOutputDevices.WriteRadioText(((char)0xBC) + "" + ((char)0xBC) + " iPod   ", CarOutputDevices.TextAlign.Center);
                    PressIPodButton();
                    PressIPodButton(true);
                    break;

                case iPodCommand.Prev:
                    CarOutputDevices.WriteRadioText(((char)0xBD) + "" + ((char)0xBD) + " iPod   ", CarOutputDevices.TextAlign.Center);
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
                            CarOutputDevices.WriteRadioText(((char)0xBC) + " VoiceOver", CarOutputDevices.TextAlign.Center);
                        }
                        else
                        {
                            IsPlaying = true; // Playing starts on VO select when paused
                        }
                    }
                    else
                    {
                        CarOutputDevices.WriteRadioText(((char)0xC9) + " VoiceOver", CarOutputDevices.TextAlign.Center);
                        PressIPodButton(false, 550); // Say current track
                    }
                    break;

                case iPodCommand.VoiceOverMenu:
                    IsInVoiceOverMenu = true;
                    break;
            }
            Debug.Print("iPod command: " + command.ToStringValue());
        }

        public static bool IsInVoiceOverMenu
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
                    CarOutputDevices.WriteRadioText(((char)0xC8) + " VoiceOver", CarOutputDevices.TextAlign.Center);
                    voiceOverMenuStarted = DateTime.Now;
                    PressIPodButton(false, 5000);
                }
                isInVoiceOverMenu = value;
            }
        }

        public static bool IsPlaying
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
                    CarOutputDevices.WriteRadioText(((char)(isPlaying ? 0xBC : 0xBE)) + " iPod  ", CarOutputDevices.TextAlign.Center);
                }
            }
        }

        static void EnqueueIPodCommand(iPodCommand command)
        {
            iPodCommands.Enqueue(command);
        }

        public static void Play()
        {
            CancelStopDelay();
            EnqueueIPodCommand(iPodCommand.Play);
        }

        public static void Pause()
        {
            CancelStopDelay();
            EnqueueIPodCommand(iPodCommand.Pause);
        }

        public static void PlayPauseToggle()
        {
            CancelStopDelay();
            EnqueueIPodCommand(iPodCommand.PlayPauseToggle);
        }

        public static void Next()
        {
            EnqueueIPodCommand(iPodCommand.Next);
        }

        public static void Prev()
        {
            EnqueueIPodCommand(iPodCommand.Prev);
        }

        public static void VoiceOverCurrent()
        {
            EnqueueIPodCommand(iPodCommand.VoiceOverCurrent);
        }

        public static void VoiceOverMenu()
        {
            EnqueueIPodCommand(iPodCommand.VoiceOverMenu);
        }

        #endregion

        #region CD-changer emulation

        static void CancelStopDelay()
        {
            if (stopDelay != null)
            {
                stopDelay.Dispose();
                stopDelay = null;
            }
        }

        public static bool IsCDCActive
        {
            get
            {
                return isCDCActive;
            }
            private set
            {
                if (isCDCActive == value)
                {
                    return;
                }
                isCDCActive = value;
                if (isCDCActive)
                {
                    Play();

                    if (announceThread.ThreadState != ThreadState.Suspended)
                    {
                        announceThread.Suspend();
                    }
                }
                else
                {
                    if (stopDelay == null)
                    {
                        // Don't pause immediately - the radio can send "start play" command soon
                        stopDelay = new Timer(delegate
                        {
                            Pause();

                            if (announceThread.ThreadState == ThreadState.Suspended)
                            {
                                announceThread.Resume();
                            }
                        }, null, StopDelayMilliseconds, 0);
                    }
                }
            }
        }

        static void ProcessCDCMessage(Message m)
        {
            if (m.Data.Compare(DataStartPlaying))
            {
                IsCDCActive = true;
            }
            else if (m.Data.Compare(DataStopPlaying))
            {
                IsCDCActive = false;
            }
            else if (m.Data.Compare(DataPollRequest))
            {
                Manager.EnqueueMessage(MessagePollResponse);
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                Debug.Print("Radio polled");
            }
            else if (m.Data.Compare(DataRandomPlay))
            {
                // Fixing IsPlaying flag, when playing iPod was connected to paused CDC
                isPlaying = !isPlaying;
                Debug.Print("Random play pressed");
            }
            else if (m.SourceDevice == DeviceAddress.Radio)
            {
                Debug.Print(m.PrettyDump);
            }
        }

        static void announce()
        {
            while (true)
            {
                Manager.EnqueueMessage(MessageAnnounce);
                Manager.EnqueueMessage(MessagePollResponse);
                Thread.Sleep(30000);
            }
        }

        #endregion
    }
}
