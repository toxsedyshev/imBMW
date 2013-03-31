using System;
using Microsoft.SPOT;

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
    }
}
