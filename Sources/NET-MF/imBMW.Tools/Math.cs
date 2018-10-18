using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public static class Math
    {
        public static byte Min(byte one, byte two)
        {
            return one < two ? one : two;
        }

        public static byte Max(byte one, byte two)
        {
            return one > two ? one : two;
        }
    }
}
