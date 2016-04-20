using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public static class Parse
    { 
        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="i">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseInt(string s, out int i)
        {
            i = 0;
            try
            {
                i = int.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="i">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseShort(string s, out short i)
        {
            i = 0;
            try
            {
                i = short.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="i">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseLong(string s, out long i)
        {
            i = 0;
            try
            {
                i = long.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="i">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseDouble(string s, out double i)
        {
            i = 0;
            try
            {
                i = double.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="val">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseBool(string s, out bool val)
        {
            val = false;
            try
            {
                if (s == "1" || s.ToUpper() == bool.TrueString.ToUpper())
                {
                    val = true;

                    return true;
                }
                if (s == "0" || s.ToUpper() == bool.FalseString.ToUpper())
                {
                    return true;
                }

                return false;

            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="i">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseUInt(string s, out uint i)
        {
            i = 0;
            try
            {
                i = uint.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="i">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseUShort(string s, out ushort i)
        {
            i = 0;
            try
            {
                i = ushort.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static ulong ParseULong(string s)
        {
            ulong i;
            if (!TryParseULong(s, out i))
                throw new ApplicationException("Failed to parse");
            return i;
        }

        /// <summary>
        /// Attempt to parse the provided string value.
        /// </summary>
        /// <param name="s">String value to be parsed</param>
        /// <param name="i">Variable to set successfully parsed value to</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseULong(string s, out ulong i)
        {
            i = 0;
            try
            {
                i = ulong.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
