using System;

namespace imBMW.Tools
{
    public static class NumHelpers
    {
        const string HexChars = "0123456789ABCDEF";

        public static String ToHex(this byte b)
        {
            return HexChars[b >> 4] + HexChars[b & 0x0F].ToString();
        }

        public static byte Invert(this byte b)
        {
            return (byte)~b;
        }

        public static bool HasBits(this byte b, byte bits)
        {
            return (b & bits) != 0;
        }

        public static bool HasBit(this byte b, byte bitIndex)
        {
            CheckByteBitIndex(bitIndex);
            return b.HasBits((byte)(1 << bitIndex));
        }

        public static byte RemoveBits(this byte b, byte bits)
        {
            return (byte)(b & bits.Invert());
        }

        public static byte RemoveBit(this byte b, byte bitIndex)
        {
            CheckByteBitIndex(bitIndex);
            return b.RemoveBits((byte)(1 << bitIndex));
        }

        public static byte AddBits(this byte b, byte bits)
        {
            return (byte)(b | bits);
        }

        public static byte AddBit(this byte b, byte bitIndex)
        {
            CheckByteBitIndex(bitIndex);
            return b.AddBits((byte)(1 << bitIndex));
        }

        static void CheckByteBitIndex(byte bitIndex)
        {
            if (bitIndex > 7)
            {
                throw new ArgumentException("bitIndex");
            }
        }
    }
}
