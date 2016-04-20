using System;
using System.Text;

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
            byte[] result = new byte[take];
            if (extend && take > array.Length)
            {
                take = array.Length;
            }
            if (take == 0)
            {
                return result;
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

        public static byte[] Skip(this byte[] array, int skip)
        {
            return array.SkipAndTake(skip, array.Length - skip);
        }

        public static bool Compare(this byte[] array1, params byte[] array2)
        {
            return array1.Compare(false, array2);
        }

        public static bool Compare(this byte[] array1, int[] anyMaskIndexes, params byte[] array2)
        {
            return array1.Compare(false, anyMaskIndexes, array2);
        }

        public static bool StartsWith(this byte[] array1, params byte[] array2)
        {
            return array1.Compare(true, array2);
        }

        static bool Compare(this byte[] array1, bool startsWith, params byte[] array2)
        {
            return Compare(array1, startsWith, null, array2);
        }

        static bool Compare(this byte[] array1, bool startsWith, int[] anyMaskIndexes, params byte[] array2)
        {
            int len2 = array2.Length;
            if ((!startsWith && len2 != array1.Length) || len2 > array1.Length)
            {
                return false;
            }
            if (len2 == 0)
            {
                return true;
            }
            for (int i = 0; i < len2; i++)
            {
                if (anyMaskIndexes != null)
                {
                    bool any = false;
                    foreach (var j in anyMaskIndexes)
                    {
                        if (j == i)
                        {
                            any = true;
                            break;
                        }
                    }
                    if (any)
                    {
                        continue;
                    }
                }
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static String ToHex(this byte[] data)
        {
            if (data.Length == 0)
            {
                return String.Empty;
            }
            if (data.Length == 1)
            {
                return data[0].ToHex();
            }
#if !MF_FRAMEWORK_VERSION_V4_1
            var sb = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                sb.Append(b.ToHex());
            }
            return sb.ToString();
#else
            String s = "";
            foreach (byte b in data)
            {
                s += b.ToHex();
            }
            return s;
#endif
        }

        public static String ToHex(this byte[] data, String spacer)
        {
            if (data.Length == 0)
            {
                return String.Empty;
            }
            if (data.Length == 1)
            {
                return data[0].ToHex();
            }
#if !MF_FRAMEWORK_VERSION_V4_1
            var sb = new StringBuilder(data.Length * 2 + (data.Length - 1) * spacer.Length);
            foreach (byte b in data)
            {
                if (sb.Length > 0)
                {
                    sb.Append(spacer);
                }
                sb.Append(b.ToHex());
            }
            return sb.ToString();
#else
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
#endif
        }

        public static String ToHex(this byte[] data, Char spacer)
        {
            if (data.Length == 0)
            {
                return String.Empty;
            }
            if (data.Length == 1)
            {
                return data[0].ToHex();
            }
#if !MF_FRAMEWORK_VERSION_V4_1
            var sb = new StringBuilder(data.Length * 2 + data.Length - 1);
            foreach (byte b in data)
            {
                if (sb.Length > 0)
                {
                    sb.Append(spacer.ToString());
                }
                sb.Append(b.ToHex());
            }
            return sb.ToString();
#else
            String s = "";
            foreach (byte b in data)
            {
                if (s.Length > 0)
                {
                    s += spacer.ToString();
                }
                s += b.ToHex();
            }
            return s;
#endif
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
            byte c;
            for (byte i = 0; i < len; i++)
            {
                if (chars[i] > 0xFF)
                {
                    c = 0xFF;
                }
                else
                {
                    c = (byte)chars[i];
                }
                data[i + alignOffset + offset] = c;
            }
        }

        public static bool Contains(this string[] array, string s)
        {
            foreach (var a in array)
            {
                if (a == s)
                {
                    return true;
                }
            }
            return false;
        }

        public static byte[] Combine(this byte[] array, params byte[] bytes)
        {
            if (array == null || array.Length == 0)
            {
                return bytes ?? new byte[0];
            }
            if (bytes == null || bytes.Length == 0)
            {
                return array ?? new byte[0];
            }
            var result = new byte[array.Length + bytes.Length];
            Array.Copy(array, result, array.Length);
            Array.Copy(bytes, 0, result, array.Length, bytes.Length);
            return result;
        }

        public static int IndexOf(this byte[] array, byte b, int skip = 0, int arrayLength = -1)
        {
            if (arrayLength < 0)
            {
                arrayLength = array.Length;
            }
            for (int i = skip; i < arrayLength; i++)
            {
                if (array[i] == b)
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool ParseFloat(this byte[] data, out float result, int offset, int length)
        {
            result = 0;
            var str = string.Empty;
            for (int i = offset; i < offset + length; i++)
            {
                str += (char)(data[i]);
            }
            double d;
            if (Parse.TryParseDouble(str, out d))
            {
                result = (float)d;
                return true;
            }
            return false;
        }

        public static bool ParseInt(this byte[] data, out int result, int offset, int length)
        {
            result = 0;
            var str = string.Empty;
            for (int i = offset; i < offset + length; i++)
            {
                str += (char)(data[i]);
            }
            int d;
            if (Parse.TryParseInt(str, out d))
            {
                result = d;
                return true;
            }
            return false;
        }
    }
}
