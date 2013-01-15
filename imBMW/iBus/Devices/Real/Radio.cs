using System;
using Microsoft.SPOT;

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


    static class Radio
    {
        const byte radioTextMaxlen = 11;

        public static void DisplayText(string s, TextAlign align = TextAlign.Left)
        {
            if (s.Length > radioTextMaxlen)
            {
                s = s.Substring(0, radioTextMaxlen);
            }
            byte offset = 0, len = (byte)s.Length;
            if (align == TextAlign.Center)
            {
                offset = (byte)((radioTextMaxlen - len) / 2);
            }
            else if (align == TextAlign.Right)
            {
                offset = (byte)(radioTextMaxlen - len);
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
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.InstrumentClusterElectronics, data));
        }
    }
}
