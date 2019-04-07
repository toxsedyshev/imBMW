using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public static class MathEx
    {
        public static byte Min(byte one, byte two)
        {
            return one < two ? one : two;
        }

        public static byte Max(byte one, byte two)
        {
            return one > two ? one : two;
        }

        public static double ToRadians(double val)
        {
            return val * (System.Math.PI / 180);
        }
    }
}
