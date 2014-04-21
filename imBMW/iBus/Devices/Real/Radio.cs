using System;
using Microsoft.SPOT;
using imBMW.Tools;
using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    public static class Radio
    {
        public const byte DisplayTextMaxLen = 11;

        const int displayTextDelay = 150;

        static Timer displayTextDelayTimer;
        static bool hasMID = false;

        public static void Init()
        {
            hasMID = Manager.FindDevice(DeviceAddress.MultiInfoDisplay);
        }

        static void ClearTimer()
        {
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }
        }

        public static void DisplayTextWithDelay(string s, TextAlign align = TextAlign.Left)
        {
            ClearTimer();

            displayTextDelayTimer = new Timer(delegate
            {
                DisplayText(s, align);
            }, null, displayTextDelay, 0);
        }

        public static void DisplayText(string s, TextAlign align = TextAlign.Left)
        {
            ClearTimer();

            if (hasMID)
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
            data = data.PadRight(0x20, DisplayTextMaxLen);
            data.PasteASCII(s, 3, DisplayTextMaxLen, align);
            Manager.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "Show text \"" + s + "\" on MID", data));
        }

        private static void DisplayTextRadio(string s, TextAlign align)
        {
            byte[] data = new byte[] { 0x23, 0x42, 0x30 };
            data = data.PadRight(0x19, DisplayTextMaxLen);
            data.PasteASCII(s, 3, DisplayTextMaxLen, align);
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.InstrumentClusterElectronics, "Show text \"" + s + "\" on the radio", data));
        }
    }
}
