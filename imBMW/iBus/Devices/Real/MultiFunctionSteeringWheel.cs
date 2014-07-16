using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public enum MFLButton
    {
        Next,
        Prev,
        RT,
        ModeRadio,
        ModeTelephone,
        Dial,
        DialLong
    }

    public delegate void MFLEventHandler(MFLButton button);

    #endregion


    class MultiFunctionSteeringWheel
    {
        static bool wasDialLongPressed;
        static bool needSkipRT;

        static Message MessagePhoneResponse = new Message(DeviceAddress.Telephone, DeviceAddress.Broadcast, 0x02, 0x00);

        /// <summary> 
        /// Emulate phone for right RT button commands
        /// </summary>
        public static bool EmulatePhone { get; set; }

        /// <summary>
        /// Use RT as button, not as radio/telephone modes toggle
        /// </summary>
        public static bool RTAsButton { get; set; }

        static MultiFunctionSteeringWheel()
        {
            // TODO receive BM volume commands
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.MultiFunctionSteeringWheel, ProcessMFLMessage);
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        static void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            if (e.CurrentIgnitionState != IgnitionState.Off && e.PreviousIgnitionState == IgnitionState.Off)
            {
                // MFL sends RT 00 signal on ignition OFF -> ACC
                needSkipRT = true;
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
            else if (m.Data.Length == 2 && m.Data[0] == 0x3B)
            {
                var btn = m.Data[1];
                switch (btn)
                {
                    case 0x01:
                        OnButtonPressed(m, MFLButton.Next);
                        break;

                    case 0x08:
                        OnButtonPressed(m, MFLButton.Prev);
                        break;

                    case 0x40:
                    case 0x00:
                        if (RTAsButton)
                        {
                            if (!needSkipRT || btn == 0x40)
                            {
                                OnButtonPressed(m, MFLButton.RT);
                            }
                            else
                            {
                                m.ReceiverDescription = "RT (skipped)";
                            }
                        }
                        else
                        {
                            OnButtonPressed(m, btn == 0x00 ? MFLButton.ModeRadio : MFLButton.ModeTelephone);
                        }
                        needSkipRT = false;
                        break;

                    case 0x80:
                        wasDialLongPressed = false;
                        m.ReceiverDescription = "Dial pressed";
                        break;

                    case 0x90:
                        wasDialLongPressed = true;
                        OnButtonPressed(m, MFLButton.DialLong);
                        break;

                    case 0xA0:
                        if (!wasDialLongPressed)
                        {
                            OnButtonPressed(m, MFLButton.Dial);
                        }
                        else
                        {
                            m.ReceiverDescription = "Dial released";
                        }
                        wasDialLongPressed = false;
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
