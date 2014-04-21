using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    public enum BordmonitorFields
    {
        /// <summary>
        /// Big text, 11 chars
        /// </summary>
        Title,
        /// <summary>
        /// Small text, 11 chars
        /// </summary>
        Status,
        /// <summary>
        /// Top right, 3 chars
        /// </summary>
        Program,
        /// <summary>
        /// One of 10 items, 23 chars
        /// </summary>
        Item,
        /// <summary>
        /// One of 5 lines, ?? items
        /// </summary>
        Line
    }

    public static class Bordmonitor
    {
        public static Message MessageRefreshScreen = new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, "Refresh screen", 0xA5, 0x60, 0x01, 0x00);
        public static Message MessageClearScreen   = new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, "Clear screen",   0x46, 0x0C);

        public static void ShowText(string s, BordmonitorFields field, int index = 0, bool check = false)
        {
            ShowText(s, TextAlign.Left, field, index, check);
        }

        public static void ShowText(string s, TextAlign align, BordmonitorFields field, int index = 0, bool check = false)
        {
            int len;
            byte[] data;
            switch (field)
            {
                case BordmonitorFields.Title:
                    len = 11;
                    data = new byte[] { 0x23, 0x62, 0x10 };
                    break;
                case BordmonitorFields.Status:
                    len = 11;
                    data = new byte[] { 0xA5, 0x62, 0x01, 0x06 };
                    break;
                case BordmonitorFields.Item:
                    len = 23;
                    index += 0x40;
                    /*if (index == 0x47)
                    {
                        index = 0x7;
                    }*/
                    data = new byte[] { 0x21, 0x60, 0x00, (byte)index };
                    break;
                default:
                    throw new Exception("TODO");
            }
            var offset = data.Length;
            data = data.PadRight(0x20, len);
            data.PasteASCII(s.UTF8ToASCII(), offset, len);
            if (check)
            {
                // TODO check 24 chars (23 + star)
                data[data.Length - 1] = 0x2A;
            }
            Manager.EnqueueMessage(new Message(iBus.DeviceAddress.Radio, iBus.DeviceAddress.GraphicsNavigationDriver, "Show message on BM: " + s, data));
        }

        public static void RefreshScreen()
        {
            Manager.EnqueueMessage(MessageRefreshScreen);
        }
    }
}
