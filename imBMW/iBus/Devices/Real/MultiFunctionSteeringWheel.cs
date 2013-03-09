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
        Dial,
        DialLong
    }

    public delegate void MFLEventHandler(MFLButton button);

    #endregion


    class MultiFunctionSteeringWheel
    {
        static bool wasDialLongPressed;
        static bool needSkipRT;

        static byte[] DataPollRequest = new byte[] { 0x01 };
        // TODO change to switch()
        static byte[] DataNextPressed = new byte[] { 0x3B, 0x01 };
        static byte[] DataPrevPressed = new byte[] { 0x3B, 0x08 };
        static byte[] DataRTPressedR = new byte[] { 0x3B, 0x40 };
        static byte[] DataRTPressedT = new byte[] { 0x3B, 0x00 };
        static byte[] DataDialPressed = new byte[] { 0x3B, 0x80 };
        static byte[] DataDialLongPressed = new byte[] { 0x3B, 0x90 };
        static byte[] DataDialReleased = new byte[] { 0x3B, 0xA0 };

        static Message MessagePhoneResponse = new Message(DeviceAddress.Telephone, DeviceAddress.Broadcast, 0x02, 0x00);

        /**
         * For right RT button commands
         */
        public static bool EmulatePhone { get; set; }

        static MultiFunctionSteeringWheel()
        {
            EmulatePhone = true;

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
            if (m.Data.Compare(DataPollRequest))
            {
                if (EmulatePhone)
                {
                    Manager.EnqueueMessage(MessagePhoneResponse);
                }
                m.ReceiverDescription = "Poll " + m.DestinationDevice.ToStringValue();
            }
            else if (m.Data.Compare(DataNextPressed))
            {
                OnButtonPressed(m, MFLButton.Next);
            }
            else if (m.Data.Compare(DataPrevPressed))
            {
                OnButtonPressed(m, MFLButton.Prev);
            }
            else if (m.Data.Compare(DataRTPressedR) || m.Data.Compare(DataRTPressedT))
            {
                if (!needSkipRT || m.Data.Compare(DataRTPressedR))
                {
                    OnButtonPressed(m, MFLButton.RT);
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
                OnButtonPressed(m, MFLButton.DialLong);
            }
            else if (m.Data.Compare(DataDialReleased))
            {
                if (!wasDialLongPressed)
                {
                    OnButtonPressed(m, MFLButton.Dial);
                }
                wasDialLongPressed = false;
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
