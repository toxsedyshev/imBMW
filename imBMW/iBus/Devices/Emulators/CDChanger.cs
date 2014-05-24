using System.Threading;
using imBMW.Tools;
using imBMW.Multimedia;

namespace imBMW.iBus.Devices.Emulators
{
    public class CDChanger : MediaEmulator
    {
        const int StopDelayMilliseconds = 1000;

        readonly Thread _announceThread;
        Timer _stopDelay;

        #region Messages

        static readonly Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        static readonly Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x01);
        static readonly Message MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playing D1 T1", 0x39, 0x02, 0x09, 0x00, 0x3F, 0x00, 0x01, 0x01); // was 39 00 09
        static readonly Message MessageStoppedDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stopped D1 T1", 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, 0x01, 0x01); // try 39 00 0C ?
        static readonly Message MessagePausedDisk1Track1  = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D1 T1",  0x39, 0x01, 0x0C, 0x00, 0x3F, 0x00, 0x01, 0x01);

        static readonly byte[] DataCurrentDiskTrackRequest = { 0x38, 0x00, 0x00 };
        static readonly byte[] DataStop  = { 0x38, 0x01, 0x00 };
        static readonly byte[] DataPause = { 0x38, 0x02, 0x00 };
        static readonly byte[] DataPlay  = { 0x38, 0x03, 0x00 };
        static readonly byte[] DataRandomPlay   = { 0x38, 0x08, 0x01 };

        #endregion

        public CDChanger(IAudioPlayer player)
            : base(player)
        {
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, ProcessCDCMessage);

            _announceThread = new Thread(Announce);
            _announceThread.Start();
        }

        #region Player control

        // TODO move to radio menu
        /*bool delayRadioText = false;

        void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        {
            string s = TextWithIcon(isPlaying ? CharIcons.Play : CharIcons.Pause);
            ShowPlayerStatus(player, s);
        }

        void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent)
        {
            if (!IsEnabled)
            {
                return;
            }
            switch (playerEvent)
            {
                case PlayerEvent.Next:
                    status = TextWithIcon(CharIcons.Next);
                    break;
                case PlayerEvent.Prev:
                    status = TextWithIcon(CharIcons.Prev);
                    break;
                case PlayerEvent.Playing:
                    status = TextWithIcon(CharIcons.Play, status);
                    break;
                case PlayerEvent.Current:
                    status = TextWithIcon(CharIcons.SelectedArrow, status);
                    break;
                case PlayerEvent.Voice:
                    status = TextWithIcon(CharIcons.Voice, status);
                    break;
            }
            ShowPlayerStatus(player, status);
        }

        string TextWithIcon(string icon, string text = null)
        {
            if (text == null)
            {
                text = player.Name;
            }
            if (icon.Length + text.Length < Radio.DisplayTextMaxLen)
            {
                return icon + " " + text;
            }
            else
            {
                return icon + text;
            }
        }

        void ShowPlayerStatus(IAudioPlayer player, string status)
        {
            if (!IsEnabled)
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
        }*/

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
            if (_stopDelay != null)
            {
                _stopDelay.Dispose();
                _stopDelay = null;
            }
        }

        protected override void OnIsEnabledChanged(bool isEnabled, bool fire = true)
        {
            //delayRadioText = true; // TODO move to radio menu
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

                if (_announceThread.ThreadState != ThreadState.Suspended)
                {
                    _announceThread.Suspend();
                }
            }

            base.OnIsEnabledChanged(isEnabled, isEnabled); // fire only if enabled

            if (!isEnabled)
            {
                CancelStopDelay();
                // Don't pause immediately - the radio can send "start play" command soon
                _stopDelay = new Timer(delegate
                {
                    FireIsEnabledChanged();
                    Pause();

                    if (_announceThread.ThreadState == ThreadState.Suspended)
                    {
                        _announceThread.Resume();
                    }
                }, null, StopDelayMilliseconds, 0);
            }
        }

        void ProcessCDCMessage(Message m)
        {
            if (m.Data.Compare(DataCurrentDiskTrackRequest))
            {
                Manager.EnqueueMessage(Player.IsPlaying ? MessagePlayingDisk1Track1 : MessagePausedDisk1Track1);
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
                Manager.EnqueueMessage(Player.IsPlaying ? MessagePlayingDisk1Track1 : MessagePausedDisk1Track1);
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
                // TODO show "splash" only with bmw business (not with BM)
                //Radio.DisplayText("imBMW", TextAlign.Center);
                m.ReceiverDescription = "Pause";
            }
            else if (m.Data[0] == 0x38)
            {
                // TODO remove
                Logger.Warning("Need response!!!");
            }
        }

        static void Announce()
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
