using System;
using imBMW.Tools;
using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegates, etc

    public delegate void RadioOnOffHandler(bool turnedOn);

    #endregion

    public static class Radio
    {
        public static byte[] DataRadioOn = new byte[] { 0x4A, 0xFF };
        public static byte[] DataRadioOff = new byte[] { 0x4A, 0x00 };

        public const byte DisplayTextMaxLength = 11;

        const int displayTextDelay = 200;

        static Timer displayTextDelayTimer;

        public static bool HasMID { get; set; }

        /// <summary>
        /// Fires on radio on/off. Only for BM54/24.
        /// </summary>
        public static event RadioOnOffHandler OnOffChanged;

        static Radio()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessRadioMessage(Message m)
        {
            var radioOnOffChanged = OnOffChanged;
            if (radioOnOffChanged != null)
            {
                if (m.Data.Compare(DataRadioOn))
                {
                    radioOnOffChanged(true);
                    m.ReceiverDescription = "Radio On";
                    return;
                }
                if (m.Data.Compare(DataRadioOff))
                {
                    radioOnOffChanged(false);
                    m.ReceiverDescription = "Radio Off";
                    return;
                }
            }
        }

        static void ClearDisplayTextTimer()
        {
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }
        }

        public static void DisplayTextWithDelay(string s, TextAlign align = TextAlign.Left, Action callback = null)
        {
            DisplayTextWithDelay(s, displayTextDelay, align, callback);
        }

        public static void DisplayTextWithDelay(string s, int delay, TextAlign align = TextAlign.Left, Action callback = null)
        {
            ClearDisplayTextTimer();

            displayTextDelayTimer = new Timer(delegate
            {
                DisplayText(s, align);
                if (callback != null)
                {
                    callback();
                }
            }, null, delay, 0);
        }

        public static void DisplayText(string s, TextAlign align = TextAlign.Left)
        {
            ClearDisplayTextTimer();

            if (HasMID)
            {
                DisplayTextMID(s, align);
            }
            else
            {
                DisplayTextRadio(s, align);
            }
        }

        private static void DisplayTextMID(string s, TextAlign align)
        {
            byte[] data = new byte[] { 0x23, 0x40, 0x20 };
            data = data.PadRight(0x20, DisplayTextMaxLength);
            data.PasteASCII(s.Translit(), 3, DisplayTextMaxLength, align);
            Manager.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "Show text \"" + s + "\" on MID", data));
            
            // TODO test that
            //if (duplicateToIKE)
            //{
            //    byte[] data = new byte[] { 0x23, 0x00, 0x20, 0x07, 0x20, 0x20, 0x20, 0x20, 0x20, 0x08 };
            //    data = data.PadRight(0x20, DisplayTextMaxLen);
            //    data.PasteASCII(s.Translit(), 10, DisplayTextMaxLen, align);
            //    Manager.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.Broadcast, "Show text \"" + s + "\" on MID and IKE", data));
            //}
        }

        private static void DisplayTextRadio(string s, TextAlign align)
        {
            byte[] data = new byte[] { 0x23, 0x42, 0x30 };
            data = data.PadRight(0xFF, DisplayTextMaxLength);
            data.PasteASCII(s.Translit(), 3, DisplayTextMaxLength, align);
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.InstrumentClusterElectronics, "Show text \"" + s + "\" on the radio", data));
        }

        /// <summary>
        /// Turns radio on/off. Only for BM54/24.
        /// </summary>
        public static void PressOnOffToggle()
        {
            Manager.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press radio on/off", 0x48, 0x06),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release radio on/off", 0x48, 0x86)
            );
        }

        /// <summary>
        /// Press Next. Only for BM54/24.
        /// </summary>
        public static void PressNext()
        {
            Manager.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Next", 0x48, 0x00),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Next", 0x48, 0x80)
           );
        }

        /// <summary>
        /// Press Prev. Only for BM54/24.
        /// </summary>
        public static void PressPrev()
        {
            Manager.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Prev", 0x48, 0x10),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Prev", 0x48, 0x90)
             );
        }

        /// <summary>
        /// Press Mode. Only for BM54/24.
        /// </summary>
        public static void PressMode()
        {
            Manager.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Mode", 0x48, 0x23),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Mode", 0x48, 0xA3)
            );
        }

        /// <summary>
        /// Press FM. Only for BM54/24.
        /// </summary>
        public static void PressFM()
        {
            Manager.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press FM", 0x48, 0x31),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release FM", 0x48, 0xB1)
            );
        }

        /// <summary>
        /// Press AM. Only for BM54/24.
        /// </summary>
        public static void PressAM()
        {
            Manager.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press AM", 0x48, 0x21),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release AM", 0x48, 0xA1)
            );
        }

        /// <summary>
        /// Press Switch Sides. Only for BM54/24.
        /// </summary>
        public static void PressSwitchSide()
        {
            Manager.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Switch Sides", 0x48, 0x14),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Switch Sides", 0x48, 0x94)
            );
        }
    }
}
