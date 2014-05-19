using System;
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
        /// Replace all occurrences of the 'find' string with the 'replace' string.
        /// </summary>
        /// <param name="content">Original string to operate on</param>
        /// <param name="find">String to find within the original string</param>
        /// <param name="replace">String to be used in place of the find string</param>
        /// <returns>Final string after all instances have been replaced.</returns>
        public static string Replace(this string content, string find, string replace)
        {
            const int startFrom = 0;
            int findItemLength = find.Length;

            int firstFound;
            var returning = new StringBuilder();

            string workingString = content;

            while ((firstFound = workingString.IndexOf(find, startFrom)) >= 0)
            {
                returning.Append(workingString.Substring(0, firstFound));
                returning.Append(replace);

                // the remaining part of the string.
                workingString = workingString.Substring(firstFound + findItemLength, workingString.Length - (firstFound + findItemLength));
            }

            returning.Append(workingString);

            return returning.ToString();
        }

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
        /// <param name="append">Char to append</param>
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
            for (var i = 0; i < s.Length; i++)
            {
                char c = s[i];
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
            foreach (var c in str.ToCharArray())
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

        /// <summary>
        /// Replaces one or more format items in a specified string with the string representation of a specified object.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="arg">The object to format.</param>
        /// <returns>A copy of format in which any format items are replaced by the string representation of arg0.</returns>
        /// <exception cref="System.ArgumentNullException">format or args is null</exception>
        public static string Format(string format, object arg)
        {
            return Format(format, new[] { arg });
        }

        private const string FormatInvalidMessage = "Format string is not valid";

        /// <summary>
        /// Format the given string using the provided collection of objects.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A copy of format in which the format items have been replaced by the string representation of the corresponding objects in args.</returns>
        /// <exception cref="System.ArgumentNullException">format or args is null</exception>
        /// <example>
        /// x = StringUtility.Format("Quick brown {0}","fox");
        /// </example>
        public static string Format(string format, params object[] args)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            if (args == null)
                throw new ArgumentNullException("args");

            // Validate the structure of the format string.
            ValidateFormatString(format);

            var bld = new StringBuilder();

            int endOfLastMatch = 0;
            int starting = 0;

            while (starting >= 0)
            {
                starting = format.IndexOf('{', starting);

                if (starting >= 0)
                {
                    if (starting != format.Length - 1)
                    {
                        if (format[starting + 1] == '{')
                        {
                            // escaped starting bracket.
                            starting = starting + 2;
                            continue;
                        }
                        bool found = false;
                        int endsearch = format.IndexOf('}', starting);

                        while (endsearch > starting)
                        {
                            if (endsearch != (format.Length - 1) && format[endsearch + 1] == '}')
                            {
                                // escaped ending bracket
                                endsearch = endsearch + 2;
                            }
                            else
                            {
                                if (starting != endOfLastMatch)
                                {
                                    string t = format.Substring(endOfLastMatch, starting - endOfLastMatch);
                                    t = t.Replace("{{", "{"); // get rid of the escaped brace
                                    t = t.Replace("}}", "}"); // get rid of the escaped brace
                                    bld.Append(t);
                                }

                                // we have a winner
                                string fmt = format.Substring(starting, endsearch - starting + 1);

                                if (fmt.Length >= 3)
                                {
                                    fmt = fmt.Substring(1, fmt.Length - 2);

                                    string[] indexFormat = fmt.Split(new[] { ':' });

                                    string formatString = string.Empty;

                                    if (indexFormat.Length == 2)
                                    {
                                        formatString = indexFormat[1];
                                    }

                                    int index;

                                    // no format, just number
                                    if (Parse.TryParseInt(indexFormat[0], out index))
                                    {
                                        bld.Append(FormatParameter(args[index], formatString));
                                    }
                                    else
                                    {
                                        throw new ApplicationException(FormatInvalidMessage);
                                    }
                                }

                                endOfLastMatch = endsearch + 1;

                                found = true;
                                starting = endsearch + 1;
                                break;
                            }


                            endsearch = format.IndexOf('}', endsearch);
                        }
                        // need to find the ending point

                        if (!found)
                        {
                            throw new ApplicationException(FormatInvalidMessage);
                        }
                    }
                    else
                    {
                        // invalid
                        throw new ApplicationException(FormatInvalidMessage);
                    }

                }

            }

            // copy any additional remaining part of the format string.
            if (endOfLastMatch != format.Length)
            {
                bld.Append(format.Substring(endOfLastMatch, format.Length - endOfLastMatch));
            }

            return bld.ToString();
        }

        private static void ValidateFormatString(string format)
        {
            char expected = '{';

            int i = 0;

            while ((i = format.IndexOfAny(new[] { '{', '}' }, i)) >= 0)
            {
                if (i < (format.Length - 1) && format[i] == format[i + 1])
                {
                    // escaped brace. continue looking.
                    i = i + 2;
                    continue;
                }
                if (format[i] != expected)
                {
                    // badly formed string.
                    throw new ApplicationException(FormatInvalidMessage);
                }
                // move it along.
                i++;
                // expected it.
                expected = expected == '{' ? '}' : '{';
            }

            if (expected == '}')
            {
                // orphaned opening brace. Bad format.
                throw new ApplicationException(FormatInvalidMessage);
            }
        }

        /// <summary>
        /// Format the provided object using the provided format string.
        /// </summary>
        /// <param name="p">Object to be formatted</param>
        /// <param name="formatString">Format string to be applied to the object</param>
        /// <returns>Formatted string for the object</returns>
        private static string FormatParameter(object p, string formatString)
        {
            if (formatString == string.Empty)
                return p.ToString();

            if (p as IFormattable != null)
            {
                return ((IFormattable)p).ToString(formatString, null);
            }
            if (p is DateTime)
            {
                return ((DateTime)p).ToString(formatString);
            }
            if (p is Double)
            {
                return ((Double)p).ToString(formatString);
            }
            if (p is Int16)
            {
                return ((Int16)p).ToString(formatString);
            }
            if (p is Int32)
            {
                return ((Int32)p).ToString(formatString);
            }
            if (p is Int64)
            {
                return ((Int64)p).ToString(formatString);
            }
            if (p is SByte)
            {
                return ((SByte)p).ToString(formatString);
            }
            if (p is Single)
            {
                return ((Single)p).ToString(formatString);
            }
            if (p is UInt16)
            {
                return ((UInt16)p).ToString(formatString);
            }
            if (p is UInt32)
            {
                return ((UInt32)p).ToString(formatString);
            }
            if (p is UInt64)
            {
                return ((UInt64)p).ToString(formatString);
            }
            return p.ToString();
        }
    }
}
