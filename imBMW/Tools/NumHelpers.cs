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

        public static bool HasBits(this byte b, byte bits)
        {
            return (b & bits) != 0;
        }

        public static bool HasBit(this byte b, byte bitIndex)
        {
            checkByteBitIndex(bitIndex);
            return b.HasBits((byte)System.Math.Pow(2, bitIndex));
        }

        public static byte RemoveBits(this byte b, byte bits)
        {
            return (byte)(b & bits.Invert());
        }

        public static byte RemoveBit(this byte b, byte bitIndex)
        {
            checkByteBitIndex(bitIndex);
            return b.RemoveBits((byte)System.Math.Pow(2, bitIndex));
        }

        public static byte AddBits(this byte b, byte bits)
        {
            return (byte)(b | bits);
        }

        public static byte AddBit(this byte b, byte bitIndex)
        {
            checkByteBitIndex(bitIndex);
            return b.AddBits((byte)System.Math.Pow(2, bitIndex));
        }

        static void checkByteBitIndex(byte bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 7)
            {
                throw new ArgumentException("bitIndex");
            }
        }
    }
}
