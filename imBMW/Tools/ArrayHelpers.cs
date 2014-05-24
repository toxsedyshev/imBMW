using System;

namespace imBMW.Tools
{
    #region Enums

    public enum TextAlign
    {
        Left,
        Right,
        Center
    }

    #endregion

    public static class ArrayHelpers
    {
        public static byte[] SkipAndTake(this byte[] array, int skip, int take, bool extend = false)
        {
            if (!extend && take > array.Length)
            {
                take = array.Length;
            }
            var result = new byte[take];
            if (extend && take > array.Length)
            {
                take = array.Length;
            }
            if (skip == 0)
            {
                Array.Copy(array, result, take);
            }
            else
            {
                Array.Copy(array, skip, result, 0, take);
            }
            return result;
        }

        public static bool Compare(this byte[] array1, params byte[] array2)
        {
            return array1.Compare(false, array2);
        }

        public static bool StartsWith(this byte[] array1, params byte[] array2)
        {
            return array1.Compare(true, array2);
        }

        static bool Compare(this byte[] array1, bool startsWith, params byte[] array2)
        {
            int len2 = array2.Length;
            if (len2 > array1.Length || (!startsWith && len2 != array1.Length))
            {
                return false;
            }
            if (len2 == 0)
            {
                return true;
            }
            if (len2 > 256)
            {
                for (int i = 0; i < len2; i++)
                {
                    if (array1[i] != array2[i])
                    {
                        return false;
                    }
                }
            }
            else
            {
                for (byte i = 0; i < len2; i++)
                {
                    if (array1[i] != array2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static String ToHex(this byte[] data)
        {
            String s = "";
            foreach (byte b in data)
            {
                s += b.ToHex();
            }
            return s;
        }

        public static String ToHex(this byte[] data, String spacer)
        {
            String s = "";
            foreach (byte b in data)
            {
                if (s.Length > 0)
                {
                    s += spacer;
                }
                s += b.ToHex();
            }
            return s;
        }

        public static String ToHex(this byte[] data, Char spacer)
        {
            String s = "";
            foreach (byte b in data)
            {
                if (s.Length > 0)
                {
                    s += spacer;
                }
                s += b.ToHex();
            }
            return s;
        }

        public static byte[] PadRight(this byte[] data, byte b, int count)
        {
            var offset = data.Length;
            data = data.SkipAndTake(0, data.Length + count, true);
            for (var i = offset; i < data.Length; i++)
            {
                data[i] = b;
            }
            return data;
        }

        public static void PasteASCII(this byte[] data, string s, int offset, int limit, TextAlign align = TextAlign.Left)
        {
            if (s == null || s.Length == 0)
            {
                return;
            }
            if (offset < 0 || offset >= data.Length)
            {
                throw new ArgumentException("offset");
            }
            if (limit < 1)
            {
                throw new ArgumentException("limit");
            }
            if (limit > data.Length - offset)
            {
                limit = data.Length - offset;
            }
            if (s.Length > limit)
            {
                s = s.Substring(0, limit);
            }
            byte alignOffset = 0, len = (byte)s.Length;
            if (align == TextAlign.Center)
            {
                alignOffset = (byte)((limit - len) / 2);
            }
            else if (align == TextAlign.Right)
            {
                alignOffset = (byte)(limit - len);
            }
            char[] chars = s.ToCharArray();
            for (byte i = 0; i < len; i++)
            {
                byte c;
                if (chars[i] > 0xff)
                {
                    c = 0x19;
                }
                else
                {
                    c = (byte)chars[i];
                }
                data[i + alignOffset + offset] = c;
            }
        }
    }
}
