using System;
using Microsoft.SPOT;
using System.Text;

namespace imBMW.Tools
{
    public static class CharIcons
    {
        public const string Play = "\xBC";
        public const string Pause = "\xBE";
        public const string Next = "\xBC\xBC";
        public const string Prev = "\xBD\xBD";
        public const string Voice = "\xC9";
        public const string SelectedArrow = "\xC8";
    }

    public static class StringHelpers
    {
        /// <summary>
        /// Prepends chars to string to reach specified length
        /// </summary>
        /// <param name="prepend">Char to prepend</param>
        /// <param name="s">String prepend to</param>
        /// <param name="length">Required string length</param>
        /// <returns></returns>
        public static string PrependToLength(this string s, uint length, char prepend = ' ')
        {
            while (s.Length < length)
            {
                s = prepend + s;
            }
            return s;
        }

        /// <summary>
        /// Appends chars to string to reach specified length
        /// </summary>
        /// <param name="prepend">Char to append</param>
        /// <param name="s">String append to</param>
        /// <param name="length">Required string length</param>
        /// <returns></returns>
        public static string AppendToLength(this string s, uint length, char append = ' ')
        {
            while (s.Length < length)
            {
                s = s + append;
            }
            return s;
        }

        public static string UTF8ToASCII(this string s)
        {
            //   C0 = А, F0 = Я, F1 = а, FF = я - ASCII
            // 0410 = А,               044F = я - UTF8
            // TODO 1025 Ё 1105 ё
            var res = new byte[s.Length];
            char c;
            for (var i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c >= 0x0410 && c <= 0x044F)
                {
                    c = (char)(c - 0x0350);
                }
                res[i] = (byte)c;
            }
            return ASCIIEncoding.GetString(res);
        }

        public static string GetString(this Encoding encoding, params byte[] bytes)
        {
            return encoding.GetString(bytes, 0, bytes.Length);
        }

        public static string GetString(this Encoding encoding, byte[] bytes, int offset, int length)
        {
            if (bytes.Length == 0 || bytes.Length == 1 && bytes[0] == 0)
            {
                return "";
            }
            return new string(encoding.GetChars(bytes, offset, length));
        }

        #if MF_FRAMEWORK_VERSION_V4_1
        public static char[] GetChars(this Encoding encoding, byte[] bytes, int offset, int length)
        {
            if (offset != 0 || length != bytes.Length)
            {
                bytes = bytes.SkipAndTake(offset, length);
            }
            return encoding.GetChars(bytes);
        }
        #endif

        public static bool IsNullOrEmpty(string str)
        {
            return str == null || str.Length == 0;
        }

        public static bool IsNumeric(this string str, bool positiveOnly = true, bool integerOnly = true)
        {
            if (IsNullOrEmpty(str))
            {
                return false;
            }
            foreach (var c in str)
            {
                if (!c.IsNumeric())
                {
                    if (c == '+')
                    {
                        continue;
                    }
                    if (c == '.' && !integerOnly)
                    {
                        continue;
                    }
                    if (c == '-' && !positiveOnly)
                    {
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }

        public static bool IsNumeric(this char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}
