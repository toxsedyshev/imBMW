using System;
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
        public static Message MessageDisableRadioMenu = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Disable radio menu", 0x45, 0x02); // Thanks to RichardP (Intravee) for these two messages
        public static Message MessageEnableRadioMenu = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Enable radio menu", 0x45, 0x00);

        public static byte[] DataRadioOn = { 0x4A, 0xFF };
        public static byte[] DataRadioOff = { 0x4A, 0x00 };
        public static byte[] DataShowTitle = { 0x23, 0x62, 0x10 };
        public static byte[] DataAUX = { 0x23, 0x62, 0x10, 0x41, 0x55, 0x58, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

        public static bool MK2Mode { get; set; }

        public static Message ShowText(string s, BordmonitorFields field, byte index = 0, bool check = false)
        {
            return ShowText(s, TextAlign.Left, field, index, check);
        }

        public static Message ShowText(string s, TextAlign align, BordmonitorFields field, byte index = 0, bool check = false)
        {
            int len;
            byte[] data;
            switch (field)
            {
                case BordmonitorFields.Title:
                    len = 11;
                    data = DataShowTitle;
                    break;
                case BordmonitorFields.Status:
                    len = 11;
                    data = new byte[] { 0xA5, 0x62, 0x01, 0x06 };
                    break;
                case BordmonitorFields.Item:
                    // TODO test MK2 length
                    len = check ? 14 : 23;
                    index += 0x40;
                    /*if (index == 0x47)
                    {
                        index = 0x7;
                    }*/
                    data = MK2Mode ? new byte[] { 0xA5, 0x62, 0x00, index } : new byte[] { 0x21, 0x60, 0x00, index };
                    break;
                default:
                    throw new Exception("TODO");
            }
            var offset = data.Length;
            data = data.PadRight(0x20, len);
            data.PasteASCII(s.UTF8ToASCII(), offset, len);
            if (check)
            {
                data[data.Length - 1] = 0x2A;
            }
            var m = new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, "Show message on BM (" + index.ToHex() + "): " + s, data);
            Manager.EnqueueMessage(m);
            return m;
        }

        public static void RefreshScreen()
        {
            Manager.EnqueueMessage(MessageRefreshScreen);
        }

        public static void DisableRadioMenu()
        {
            Manager.EnqueueMessage(MessageDisableRadioMenu);
        }

        public static void EnableRadioMenu()
        {
            Manager.EnqueueMessage(MessageEnableRadioMenu);
        }
    }
}
