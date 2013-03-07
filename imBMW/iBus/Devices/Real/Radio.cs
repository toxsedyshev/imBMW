using System;
using Microsoft.SPOT;
using imBMW.Tools;
using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    #region Enums

    public enum TextAlign
    {
        Left,
        Right,
        Center
    }

    #endregion


    public static class Radio
    {
        const byte displayTextMaxlen = 11;
        const int displayTextDelay = 100;

        public static void DisplayTextWithDelay(string s, TextAlign align = TextAlign.Left)
        {
            new Timer(delegate
            {
                DisplayText(s, align);
            }, null, displayTextDelay, 0);
        }

        public static void DisplayText(string s, TextAlign align = TextAlign.Left)
        {
            if (s.Length > displayTextMaxlen)
            {
                s = s.Substring(0, displayTextMaxlen);
            }
            byte offset = 0, len = (byte)s.Length;
            if (align == TextAlign.Center)
            {
                offset = (byte)((displayTextMaxlen - len) / 2);
            }
            else if (align == TextAlign.Right)
            {
                offset = (byte)(displayTextMaxlen - len);
            }
            byte[] data = new byte[] { 0x23, 0x42, 0x30, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19 };
            char[] chars = s.ToCharArray();
            byte c;
            for (byte i = 0; i < len; i++)
            {
                if (chars[i] > 0xff) {
                    c = 0x19;
                } else {
                    c = (byte)chars[i];
                }
                data[i + offset + 3] = c;
            }
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.InstrumentClusterElectronics, "Show text on radio \"" + s + "\"", data));
            Logger.Info("Display on the radio \"" + s + "\"");
        }
    }
}
