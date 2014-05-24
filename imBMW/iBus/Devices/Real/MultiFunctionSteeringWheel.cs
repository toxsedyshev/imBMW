using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public enum MFLButton
    {
        Next,
        NextHold,
        Prev,
        PrevHold,
        VolumeUp,
        VolumeDown,
        RT,
        RTRelease,
        Dial,
        DialLong
    }

    public delegate void MFLEventHandler(MFLButton button);

    #endregion

    class MultiFunctionSteeringWheel
    {
        static bool _wasDialLongPressed;
        static bool _wasNextLongPressed;
        static bool _wasPrevLongPressed;
        static bool _needSkipRt;

        static readonly Message MessagePhoneResponse = new Message(DeviceAddress.Telephone, DeviceAddress.Broadcast, 0x02, 0x00);

        /**
         * For right RT button commands
         */
        public static bool EmulatePhone { get; set; }

        static MultiFunctionSteeringWheel()
        {
            EmulatePhone = true;

            // TODO receive BM volume commands
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.MultiFunctionSteeringWheel, ProcessMFLMessage);
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        static void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            if (e.CurrentIgnitionState != IgnitionState.Off && e.PreviousIgnitionState == IgnitionState.Off)
            {
                // MFL sends RT 00 signal on ignition OFF -> ACC
                _needSkipRt = true;
            }
        }

        static void ProcessMFLMessage(Message m)
        {
            if (m.Data.Compare(MessageRegistry.DataPollRequest))
            {
                if (EmulatePhone)
                {
                    Manager.EnqueueMessage(MessagePhoneResponse);
                }
            }
            else if (m.Data.Length == 2 && m.Data[0] == 0x32)
            {
                var btn = m.Data[1];
                switch (btn)
                {
                    case 0x10:
                        OnButtonPressed(m, MFLButton.VolumeDown);
                        break;
                    case 0x11:
                        OnButtonPressed(m, MFLButton.VolumeUp);
                        break;
                }
            }
            else if (m.Data.Length == 2 && m.Data[0] == 0x3B)
            {
                var btn = m.Data[1];
                switch (btn)
                {
                    case 0x01:
                        _wasNextLongPressed = false;
                        m.ReceiverDescription = "Next pressed";
                        break;
                    case 0x11:
                        _wasNextLongPressed = true;
                        OnButtonPressed(m, MFLButton.NextHold);
                        m.ReceiverDescription = "Next long pressed";
                        break;
                    case 0x21:
                        if (!_wasNextLongPressed)
                        {
                            OnButtonPressed(m, MFLButton.Next);
                        }
                        else
                        {
                            m.ReceiverDescription = "Next released";
                        }
                        _wasNextLongPressed = false;
                        break;
                    case 0x08:
                        _wasPrevLongPressed = false;
                        break;
                    case 0x18:
                        _wasPrevLongPressed = true;
                        OnButtonPressed(m, MFLButton.PrevHold);
                        break;
                    case 0x28:
                        if (!_wasPrevLongPressed)
                        {
                            OnButtonPressed(m, MFLButton.Prev);
                        }
                        else
                        {
                            m.ReceiverDescription = "Prev released";
                        }
                        _wasPrevLongPressed = false;
                        break;
                    case 0x40:
                    case 0x00:
                    case 0x02:
                    case 0x12:
                        if (!_needSkipRt || (btn == 0x40 || btn == 0x12))
                        {
                            OnButtonPressed(m, MFLButton.RT);
                        }
                        else
                        {
                            m.ReceiverDescription = "RT (skipped)";
                        }
                        _needSkipRt = false;
                        break;
                    case 0x22:
                        if (!_needSkipRt)
                        {
                            OnButtonPressed(m, MFLButton.RTRelease);
                        }
                        break;
                    case 0x80:
                        _wasDialLongPressed = false;
                        m.ReceiverDescription = "Dial pressed";
                        break;
                    case 0x90:
                        _wasDialLongPressed = true;
                        OnButtonPressed(m, MFLButton.DialLong);
                        m.ReceiverDescription = "Dial long pressed";
                        break;
                    case 0xA0:
                        if (!_wasDialLongPressed)
                        {
                            OnButtonPressed(m, MFLButton.Dial);
                        }
                        else
                        {
                            m.ReceiverDescription = "Dial released";
                        }
                        _wasDialLongPressed = false;
                        break;
                    default:
                        m.ReceiverDescription = "Button unknown " + btn.ToHex();
                        break;
                }
            }
        }

        static void OnButtonPressed(Message m, MFLButton button)
        {
            var e = ButtonPressed;
            if (e != null)
            {
                e(button);
            }
            m.ReceiverDescription = "Button " + button.ToStringValue();
        }

        public static event MFLEventHandler ButtonPressed;
    }
}
