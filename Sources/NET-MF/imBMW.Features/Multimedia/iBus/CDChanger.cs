using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;

namespace imBMW.iBus.Devices.Emulators
{
    public class CDChanger : MediaEmulator
    {
        const int StopDelayMilliseconds = 1000;
        
        Timer stopDelay;

        #region Messages
        
        Message MessagePlayingDisk1Track1;
        Message MessageStoppedDisk1Track1;
        Message MessagePausedDisk1Track1;

        static Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        
        static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };
        static byte[] DataStop  = new byte[] { 0x38, 0x01, 0x00 };
        static byte[] DataPause = new byte[] { 0x38, 0x02, 0x00 };
        static byte[] DataPlay  = new byte[] { 0x38, 0x03, 0x00 };
        static byte[] DataRandomPlay = new byte[] { 0x38, 0x08, 0x01 };

        #endregion

        public CDChanger(IAudioPlayer player, bool oneDisk = false)
            : base(player)
        {
            var disks = (byte)(oneDisk ? 0x01 : 0x3F);
            MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playing D1 T1", 0x39, 0x02, 0x09, 0x00, disks, 0x00, 0x01, 0x01);
            MessageStoppedDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stopped D1 T1", 0x39, 0x00, 0x0C, 0x00, disks, 0x00, 0x01, 0x01);
            MessagePausedDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D1 T1", 0x39, 0x01, 0x0C, 0x00, disks, 0x00, 0x01, 0x01);

            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, ProcessCDCMessage);
        }

        #region Player control
        
        protected override void Play()
        {
            CancelStopDelay();
            base.Play();
        }

        protected override void Pause()
        {
            CancelStopDelay();
            base.Pause();
        }

        protected override void PlayPauseToggle()
        {
            CancelStopDelay();
            base.PlayPauseToggle();
        }

        #endregion

        #region CD-changer emulation

        void CancelStopDelay()
        {
            if (stopDelay != null)
            {
                stopDelay.Dispose();
                stopDelay = null;
            }
        }

        protected override void OnIsEnabledChanged(bool isEnabled, bool fire = true)
        {
            Player.PlayerHostState = isEnabled ? PlayerHostState.On : PlayerHostState.Off;
            if (isEnabled)
            {
                if (Player.IsPlaying)
                {
                    // Already playing - CDC turning off cancelled
                    //ShowPlayerStatus(player, player.IsPlaying); // TODO need it?
                }
                else
                {
                    Play();
                }
            }

            base.OnIsEnabledChanged(isEnabled, isEnabled); // fire only if enabled

            if (!isEnabled)
            {
                CancelStopDelay();
                // Don't pause immediately - the radio can send "start play" command soon
                stopDelay = new Timer(delegate
                {
                    FireIsEnabledChanged();
                    Pause();
                }, null, StopDelayMilliseconds, 0);
            }
        }

        void ProcessCDCMessage(Message m)
        {
            if (m.Data.Compare(DataCurrentDiskTrackRequest))
            {
                if (Player.IsPlaying)
                {
                    Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                }
                else
                {
                    Manager.EnqueueMessage(MessagePausedDisk1Track1);
                }
                m.ReceiverDescription = "CD status request";
            }
            else if (m.Data.Compare(DataPlay))
            {
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                IsEnabled = true;
                m.ReceiverDescription = "Start playing";
            }
            else if (m.Data.Compare(DataStop))
            {
                Manager.EnqueueMessage(MessageStoppedDisk1Track1);
                IsEnabled = false;
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
                    Manager.EnqueueMessage(MessagePausedDisk1Track1);
                }
            }
            else if (m.Data.Compare(DataRandomPlay))
            {
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                RandomToggle();
                m.ReceiverDescription = "Random toggle";
            }
            else if (m.Data.Compare(DataPause))
            {
                //IsEnabled = false;
                Manager.EnqueueMessage(MessagePausedDisk1Track1);
                m.ReceiverDescription = "Pause";
            }
            else if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x0A)
            {
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                switch (m.Data[2])
                {
                    case 0x00:
                        Player.Next();
                        m.ReceiverDescription = "Next track";
                        break;
                    case 0x01:
                        Player.Prev();
                        m.ReceiverDescription = "Prev track";
                        break;
                }
            }
            /*else if (m.Data[0] == 0x38)
            {
                // TODO remove
                Logger.Warning("Need response!!!");
            }*/
        }

        #endregion
    }
}
