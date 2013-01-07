using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus.Devices
{
    static class iPodChanger
    {
        static OutputPort iPod;
        static Thread announceThread;
        static QueueThreadWorker iPodCommands;

        static bool isPlaying;
        static bool isInVoiceOver;

        #region Messages

        static Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        static Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x01);
        static Message MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, 0x39, 0x00, 0x09, 0x00, 0x3F, 0x00, 0x01, 0x01);

        static byte[] DataPollRequest = new byte[] { 0x01 };
        static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };

        #endregion

        public static void Init(Cpu.Pin headsetControl)
        {
            iPod = new OutputPort(headsetControl, false);

            iPodCommands = new QueueThreadWorker(ExecuteIPodCommand);

            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, processMessage);

            announceThread = new Thread(announce);
            announceThread.Start();
        }

        #region iPod control

        enum iPodCommand
        {
            Play,
            Pause,
            PlayPauseToggle,
            Next,
            Prev,
            VoiceOverCurrent,
            VoiceOverMenu,
            VoiceOverSelect
        }

        static void PressIPodButton(int milliseconds)
        {
            iPod.Write(true);
            Thread.Sleep(milliseconds);
            iPod.Write(false);
            Thread.Sleep(25); // Don't flood
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
                    PressIPodButton(50);
                    PressIPodButton(50);
                    Thread.Sleep(275); // Don't flood
                    break;

                case iPodCommand.Prev:
                    PressIPodButton(50);
                    PressIPodButton(50);
                    PressIPodButton(50);
                    Thread.Sleep(275); // Don't flood
                    break;

                case iPodCommand.VoiceOverCurrent:
                    if (isInVoiceOver)
                    {
                        PressIPodButton(50);
                        isInVoiceOver = false;
                        isPlaying = true; // Playing starts on VO select when paused
                    }
                    else
                    {
                        PressIPodButton(550);
                    }
                    break;

                case iPodCommand.VoiceOverMenu:
                    PressIPodButton(5000);
                    isInVoiceOver = true;
                    break;
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
                PressIPodButton(50);
                Thread.Sleep(275); // Don't flood
                isPlaying = value;
                isInVoiceOver = false;
            }
        }

        static void EnqueueIPodCommand(iPodCommand command)
        {
            iPodCommands.Enqueue(command);
        }

        public static void Play()
        {
            EnqueueIPodCommand(iPodCommand.Play);
        }

        public static void Pause()
        {
            EnqueueIPodCommand(iPodCommand.Pause);
        }

        public static void PlayPauseToggle()
        {
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

        public static void VoiceOverSelect()
        {
            EnqueueIPodCommand(iPodCommand.VoiceOverSelect);
        }

        #endregion

        #region CD-changer emulation

        static void processMessage(Message m)
        {
            if (m.Data.Compare(MessageAnnounce.Data))
            {
                if (announceThread.ThreadState == ThreadState.Suspended)
                {
                    announceThread.Resume();
                }
                Debug.Print("iBus activated");
            }
            else if (m.Data.Compare(DataPollRequest))
            {
                /*if (announceThread.ThreadState != ThreadState.Suspended)
                {
                    announceThread.Suspend();
                }*/

                Manager.EnqueueMessage(MessagePollResponse);

                //Thread.Sleep(50);
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);

                Debug.Print("Radio polled");
            }
            else if (m.Data.Compare(DataCurrentDiskTrackRequest))
            {
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                Debug.Print("Radio requested disk&track");
            }
            else if(m.SourceDevice == DeviceAddress.Radio)
            {
                Debug.Print(m.PrettyDump);
            }
        }

        static void announce()
        {
            while (true)
            {
                Manager.EnqueueMessage(MessageAnnounce);

                Thread.Sleep(50);
                Manager.EnqueueMessage(MessagePollResponse);

                Thread.Sleep(30000);
            }
        }

        #endregion
    }
}
