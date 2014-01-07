using System;
using Microsoft.SPOT;
using System.Text;

namespace imBMW.Tools
{
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

        public static string GetString(this Encoding encoding, params byte[] bytes)
        {
            return new string(encoding.GetChars(bytes));
        }

        public static bool IsNullOrEmpty(string str)
        {
            return str == null || str.Length == 0;
        }
    }
}
