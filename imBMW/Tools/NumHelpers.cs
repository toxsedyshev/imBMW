using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public static class NumHelpers
    {
        const string hexChars = "0123456789ABCDEF";

        public static String ToHex(this byte b)
        {
            return hexChars[b >> 4].ToString() + hexChars[b & 0x0F].ToString();
        }

        public static byte Invert(this byte b)
        {
            return (byte)(0xFF - b);
        }

        public static int Invert(this int i)
        {
            return (int)(0xFFFFFFFF - i);
        }
    }
}
