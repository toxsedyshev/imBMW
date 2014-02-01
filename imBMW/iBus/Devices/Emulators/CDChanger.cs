using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;

namespace imBMW.iBus.Devices.Emulators
{
    public static class CDChanger
    {
        const int StopDelayMilliseconds = 1000;

        static IAudioPlayer player;
        static Thread announceThread;
        static Timer stopDelay;

        static bool isCDCActive;

        #region Messages

        static Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        static Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x01);
        static Message MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playing D1 T1", 0x39, 0x00, 0x09, 0x00, 0x3F, 0x00, 0x01, 0x01);
        static Message MessageStoppedDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stopped D1 T1", 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, 0x01, 0x01);

        static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };
        static byte[] DataStopPlaying  = new byte[] { 0x38, 0x01, 0x00 };
        static byte[] DataTurnOff      = new byte[] { 0x38, 0x02, 0x00 };
        static byte[] DataStartPlaying = new byte[] { 0x38, 0x03, 0x00 };
        static byte[] DataRandomPlay   = new byte[] { 0x38, 0x08, 0x01 };

        #endregion

        public static void Init(IAudioPlayer player)
        {
            Player = player;

            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, ProcessCDCMessage);
            MultiFunctionSteeringWheel.ButtonPressed += MultiFunctionSteeringWheel_ButtonPressed;

            announceThread = new Thread(announce);
            announceThread.Start();
        }

        #region Player control

        internal static IAudioPlayer Player
        {
            get
            {
                return player;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (player != null)
                {
                    UnsetPlayer(player);
                }
                player = value;
                SetupPlayer(value);
            }
        }

        static void SetupPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = true;
            player.IsPlayingChanged += ShowPlayerStatus;
            player.StatusChanged += ShowPlayerStatus;
        }

        static void UnsetPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = false;
            player.IsPlayingChanged -= ShowPlayerStatus;
            player.StatusChanged -= ShowPlayerStatus;
        }

        static bool delayRadioText = false;

        static void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        {
            string s = (isPlaying ? CharIcons.Play : CharIcons.Pause) + " " + player.Name + "  ";
            ShowPlayerStatus(player, s);
        }

        static void ShowPlayerStatus(IAudioPlayer player, string status)
        {
            if (!IsCDCActive)
            {
                return;
            }
            if (status.Length > Radio.DisplayTextMaxLen)
            {
                status = status.Substring(status.Length - Radio.DisplayTextMaxLen);
            }
            if (delayRadioText)
            {
                Radio.DisplayTextWithDelay(status, TextAlign.Center);
            }
            else
            {
                Radio.DisplayText(status, TextAlign.Center);
            }
            delayRadioText = false;
        }

        static void MultiFunctionSteeringWheel_ButtonPressed(MFLButton button)
        {
            if (!IsCDCActive)
            {
                return;
            }
            switch (button)
            {
                case MFLButton.Next:
                    Next();
                    break;
                case MFLButton.Prev:
                    Prev();
                    break;
                case MFLButton.RT:
                    MFLRT();
                    break;
                case MFLButton.Dial:
                    MFLDial();
                    break;
                case MFLButton.DialLong:
                    MFLDialLong();
                    break;
            }
        }

        static void Play()
        {
            CancelStopDelay();
            Player.Play();
        }

        static void Pause()
        {
            CancelStopDelay();
            Player.Pause();
        }

        static void PlayPauseToggle()
        {
            CancelStopDelay();
            Player.PlayPauseToggle();
        }

        static void Next()
        {
            Player.Next();
        }

        static void Prev()
        {
            Player.Prev();
        }

        static void MFLRT()
        {
            Player.MFLRT();
        }

        static void MFLDial()
        {
            Player.MFLDial();
        }

        static void MFLDialLong()
        {
            Player.MFLDialLong();
        }

        static void RandomToggle()
        {
            bool rnd = Player.RandomToggle();
            // TODO send rnd status to radio
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
                delayRadioText = true;
                player.IsPlayerHostActive = isCDCActive;
                if (isCDCActive)
                {
                    if (player.IsPlaying)
                    {
                        // Already playing - CDC turning off cancelled
                        ShowPlayerStatus(player, player.IsPlaying);
                    }
                    else
                    {
                        Play();
                    }

                    if (announceThread.ThreadState != ThreadState.Suspended)
                    {
                        announceThread.Suspend();
                    }
                }
                else
                {
                    CancelStopDelay();
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

        static void ProcessCDCMessage(Message m)
        {
            if (m.Data.Compare(DataStartPlaying))
            {
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                IsCDCActive = true;
                m.ReceiverDescription = "Start playing";
            }
            else if (m.Data.Compare(DataStopPlaying))
            {
                IsCDCActive = false;
                m.ReceiverDescription = "Stop playing";
            }
            else if (m.Data.Compare(MessageRegistry.DataPollRequest))
            {
                Manager.EnqueueMessage(MessagePollResponse);
                if (Player.IsPlaying)
                {
                    Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                }
                else
                {
                    Manager.EnqueueMessage(MessageStoppedDisk1Track1);
                }
            }
            else if (m.Data.Compare(DataRandomPlay))
            {
                RandomToggle();
                m.ReceiverDescription = "Random toggle";
            }
            else if (m.Data.Compare(DataTurnOff))
            {
                Radio.DisplayText("imBMW", TextAlign.Center);
                m.ReceiverDescription = "Turn off";
            }
        }

        static void announce()
        {
            while (true)
            {
                Manager.EnqueueMessage(MessageAnnounce, MessagePollResponse);
                Thread.Sleep(30000);
            }
        }

        #endregion
    }
}
