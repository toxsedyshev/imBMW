using System;
using Microsoft.SPOT;
using System.Text;

namespace imBMW.Tools
{
    public static class CharIcons
    {
        public const char Play = '\xBC';
        public const char Pause = '\xBE';
        public const string Next = ">>";//"\xBC\xBC";
        public const string Prev = "<<";//"\xBD\xBD";
    }

    public static class StringHelpers
    {
        /// <summary>
        /// Prepends chars to string to reach specified length
        /// </summary>
        /// <param name="s">string prepend to</param>
        /// <param name="prepend">char to prepend</param>
        /// <param name="length">required string length</param>
        /// <returns></returns>
        public static string PrependToLength(this string s, char prepend, uint length)
        {
            while (s.Length < length)
            {
                s = prepend + s;
            }
            return s;
        }

        public static string UTF8ToASCII(this string s)
        {
            //   C0 = À, F0 = ß, F1 = à, FF = ÿ - ASCII
            // 0410 = À,               044F = ÿ - UTF8
            // TODO 1025 ¨ 1105 ¸
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
            if (bytes.Length == 0 || bytes.Length == 1 && bytes[0] == 0)
            {
                return "";
            }
            return new string(encoding.GetChars(bytes));
        }

        public static bool IsNullOrEmpty(string str)
        {
            return str == null || str.Length == 0;
        }

        public static bool IsNumeric(this string str, bool positive = true, bool integer = true)
        {
            if (IsNullOrEmpty(str))
            {
                return false;
            }
            foreach (var c in str)
            {
                if (c < '0' || c > '9')
                {
                    if (c == '.' && !integer)
                    {
                        continue;
                    }
                    if (c == '-' && !positive)
                    {
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }

		public static ulong ToUlong(this string hex)
		{
			if (IsNullOrEmpty(hex)) throw new ArgumentException("hex");

			int i = hex.Length > 1 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X') ? 2 : 0;
			ulong value = 0;

			while (i < hex.Length)
			{
				uint x = hex[i++];

				if (x >= '0' && x <= '9') x = x - '0';
				else if (x >= 'A' && x <= 'F') x = (x - 'A') + 10;
				else if (x >= 'a' && x <= 'f') x = (x - 'a') + 10;
				else throw new ArgumentOutOfRangeException("hex");

				value = 16 * value + x;

			}

			return value;
		}
    }
}
