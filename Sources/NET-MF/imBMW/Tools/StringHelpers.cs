using System;
using System.Text;

namespace imBMW.Tools
{
    public static class CharIcons
    {
        public const char Play = '\xBC';
        public const char Pause = '\xBE';
        public const string Next = "\xBC\xBC";
        public const string Prev = "\xBD\xBD";
        public const char Voice = '\xC8';
        public const char SelectedArrow = '\xC9';
        public const char LeftArrow = '\xCA';
        public const char Degree = '\xA8';
        public const char Bull = '\xC3';
        public const char VertLine = '\xC0';
        public const char NetRect = '\xCB';
        public const char Net = '\xCC';
        public const char Rect = '\xB2';
        public const char BordmonitorBull = '\xB7';
    }

    public enum CharType
    {
        None,
        Upper,
        Lower,
        UpperYo,
        LowerYo
    }

    public static class StringHelpers
    {
        static object[] translitTo = new object[] { 'A', 'B', 'V', 'G', 'D', 'E', 'G', 'Z', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'R', 'S', 'T', 'U', 'F', "Kh", 'C', "Ch", "Sh", "Sh'", '"', 'Y', '\'', "Ye", "Yu", "Ya", "Yo" };

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

        public static bool IsRussianASCIIChar(this char c)
        {
            return c >= '\xC0' && c <= '\xFF';
        }

        public static CharType GetRussianCharType(this char c)
        {
            if (c >= 0x0410 && c <= 0x042F)
            {
                return CharType.Upper;
            }
            if (c >= 0x0430 && c <= 0x044F)
            {
                return CharType.Lower;
            }
            if (c == 0x1025)
            {
                return CharType.Upper;
            }
            if (c == 0x1105)
            {
                return CharType.Lower;
            }
            return CharType.None;
        }

        public static string Translit(this string s)
        {
            var found = false;
            foreach (var c in s)
            {
                if (c.GetRussianCharType() != CharType.None)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return s;
            }

            var r = "";
            CharType t;
            foreach (var c in s)
            {
                t = c.GetRussianCharType();
                if (t == CharType.None)
                {
                    r += c;
                    continue;
                }
                int i;
                switch (t)
                {
                    case CharType.Upper:
                        i = c - 0x0410;
                        break;
                    case CharType.Lower:
                        i = c - 0x0430;
                        break;
                    case CharType.UpperYo:
                    case CharType.LowerYo:
                        i = 32;
                        break;
                    default:
                        throw new Exception("Unknown char for translit.");
                }
                var nc = translitTo[i];
                if (nc is string)
                {
                    if (t == CharType.Lower || t == CharType.LowerYo)
                    {
                        nc = ((string)nc).ToLower();
                    }
                    r += (string)nc;
                }
                else
                {
                    if (t == CharType.Lower || t == CharType.LowerYo)
                    {
                        nc = ((char)nc).ToLower();
                    }
                    r += (char)nc;
                }
            }
            return r;
        }

        #if !NETMF || MF_FRAMEWORK_VERSION_V4_1
        public static char ToLower(this char c)
        {
            return c.ToString().ToLower()[0];
        }
        #endif

        public static string UTF8ToASCII(this string s)
        {
            var found = false;
            foreach (var ch in s)
            {
                if (ch > 0xFF)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return s;
            }

            //  C0 = А,  DF = Я,  E0 = а,  FF = я - ASCII
            // 410 = А, 42F = Я, 430 = а, 44F = я - UTF8
            // + 1025 = Ё, 1105 = ё
            var res = new char[s.Length];
            char c;
            for (var i = 0; i < s.Length; i++)
            {
                c = s[i];
                var t = c.GetRussianCharType();
                switch (t)
                {
                    case CharType.Upper:
                    case CharType.Lower:
                        c = (char)(c - 0x0350);
                        break;
                    case CharType.UpperYo:
                        c = '\xC5';
                        break;
                    case CharType.LowerYo:
                        c = '\xE5';
                        break;
                    default:
                        if (c > 0xFF)
                        {
                            c = ' ';
                        }
                        break;
                }
                res[i] = c;
            }
            return new string(res);
        }

        public static string ASCIIToUTF8(this string s)
        {
            var found = false;
            foreach (var ch in s)
            {
                if (ch.IsRussianASCIIChar())
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return s;
            }

            var res = new char[s.Length];
            char c;
            for (var i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c.IsRussianASCIIChar())
                {
                    res[i] = (char)(c + 0x0350);
                }
                else
                {
                    res[i] = c;
                }
            }
            return new string(res);
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
