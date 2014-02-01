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

        public static void DisplayTextWithDelay(string s, TextAlign align = TextAlign.Left)
        {
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }

            displayTextDelayTimer = new Timer(delegate
            {
                DisplayText(s, align);
            }, null, displayTextDelay, 0);
        }

        public static void DisplayText(string s, TextAlign align = TextAlign.Left)
        {
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }

            byte[] data = new byte[] { 0x23, 0x42, 0x30 };
            data = data.PadRight(0x19, DisplayTextMaxLen);
            data.PasteASCII(s, 3, DisplayTextMaxLen, align);
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.InstrumentClusterElectronics, "Show text \"" + s + "\" on the radio", data));
        }
    }
}
