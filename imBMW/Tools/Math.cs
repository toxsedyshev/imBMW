using System;

namespace imBMW.Tools
{
    public class Math
    {
        public static readonly double PI = System.Math.PI;
        public static readonly double E = System.Math.E;

        const double Sq2P1 = 2.414213562373095048802e0F;
        const double Sq2M1 = .414213562373095048802e0F;
        const double Pio2 = 1.570796326794896619231e0F;
        const double Pio4 = .785398163397448309615e0F;
        const double AtanP4 = .161536412982230228262e2F;
        const double AtanP3 = .26842548195503973794141e3F;
        const double AtanP2 = .11530293515404850115428136e4F;
        const double AtanP1 = .178040631643319697105464587e4F;
        const double AtanP0 = .89678597403663861959987488e3F;
        const double AtanQ4 = .5895697050844462222791e2F;
        const double AtanQ3 = .536265374031215315104235e3F;
        const double AtanQ2 = .16667838148816337184521798e4F;
        const double AtanQ1 = .207933497444540981287275926e4F;
        const double AtanQ0 = .89678597403663861962481162e3F;

        public static double Pow(double x, double y)
        {
            return System.Math.Pow(x, y);
        }

        public static double Sqrt(double x)
        {
            return System.Math.Pow(x, 0.5);
        }

        public static double Atan(double x)
        {
            return x > 0.0F ? Atans(x) : -Atans(-x);
        }

        public static double Asin(double x)
        {
            double sign = 1.0F;

            if (x < 0.0F)
            {
                x = -x;
                sign = -1.0F;
            }

            if (x > 1.0F)
            {
                throw new ArgumentOutOfRangeException("x");
            }

            double temp = Sqrt(1.0F - (x * x));

            if (x > 0.7)
            {
                temp = Pio2 - Atan(temp / x);
            }
            else
            {
                temp = Atan(x / temp);
            }

            return (sign * temp);
        }

        public static double Cos(double x)
        {
            // This function is based on the work described in
            // http://www.ganssle.com/approx/approx.pdf

            x = x % (PI * 2.0);

            // Make X positive if negative
            if (x < 0) { x = 0.0F - x; }

            // Get quadrand

            // Quadrand 0,  >-- Pi/2
            byte quadrand = 0;

            // Quadrand 1, Pi/2 -- Pi
            if ((x > (PI / 2F)) & (x < (PI)))
            {
                quadrand = 1;
                x = PI - x;
            }

            // Quadrand 2, Pi -- 3Pi/2
            if ((x > (PI)) & (x < ((3F * PI) / 2)))
            {
                quadrand = 2;
                x = PI - x;
            }

            // Quadrand 3 - 3Pi/2 -->
            if ((x > ((3F * PI) / 2)))
            {
                quadrand = 3;
                x = (2F * PI) - x;
            }

            // Constants used for approximation
            const double c1 = 0.99999999999925182;
            const double c2 = -0.49999999997024012;
            const double c3 = 0.041666666473384543;
            const double c4 = -0.001388888418000423;
            const double c5 = 0.0000248010406484558;
            const double c6 = -0.0000002752469638432;
            const double c7 = 0.0000000019907856854;

            // X squared
            double x2 = x * x;

            // Check quadrand
            if ((quadrand == 0) | (quadrand == 3))
            {
                // Return positive for quadrand 0, 3
                return (c1 + x2 * (c2 + x2 * (c3 + x2 * (c4 + x2 * (c5 + x2 * (c6 + c7 * x2))))));
            }
            // Return negative for quadrand 1, 2
            return 0.0F - (c1 + x2 * (c2 + x2 * (c3 + x2 * (c4 + x2 * (c5 + x2 * (c6 + c7 * x2))))));
        }

        public static double Sin(double x)
        {
            return Cos((PI / 2.0F) - x);
        }

        public static double Max(double x, double y)
        {
            return x >= y ? x : y;
        }

        public static double Min(double x, double y)
        {
            return x <= y ? x : y;
        }

        public static double ToRadians(double val)
        {
            return val * (PI / 180);
        }

        private static double Atans(double x)
        {
            return x < Sq2M1 ? Atanx(x) : (x > Sq2P1 ? Pio2 - Atanx(1.0F / x) : Pio4 + Atanx((x - 1.0F) / (x + 1.0F)));
        }

        private static double Atanx(double x)
        {
            double argsq = x * x;
            double value = ((((AtanP4 * argsq + AtanP3) * argsq + AtanP2) * argsq + AtanP1) * argsq + AtanP0);
            value = value / (((((argsq + AtanQ4) * argsq + AtanQ3) * argsq + AtanQ2) * argsq + AtanQ1) * argsq + AtanQ0);
            return (value * x);
        }
    }
}
