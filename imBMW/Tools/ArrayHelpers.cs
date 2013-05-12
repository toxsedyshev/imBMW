using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public static class ArrayHelpers
    {
        public static byte[] SkipAndTake(this byte[] array, int skip, int take)
        {
            byte[] result = new byte[take];
            Array.Copy(array, skip, result, 0, take);
            return result;
        }

        public static bool Compare(this byte[] array1, params byte[] array2)
        {
            int len1 = array1.Length;
            if (len1 != array2.Length)
            {
                return false;
            }
            if (len1 == 0)
            {
                return true;
            }
            if (len1 > 256)
            {
                for (int i = 0; i < len1; i++)
                {
                    if (array1[i] != array2[i])
                    {
                        return false;
                    }
                }
            }
            else
            {
                for (byte i = 0; i < len1; i++)
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
    }
}
