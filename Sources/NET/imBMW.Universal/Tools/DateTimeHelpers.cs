using System;

namespace imBMW.Tools
{
    public static class DateTimeHelpers
    {
        public static int GetTotalHours(this TimeSpan timespan)
        {
            return (int)timespan.TotalHours;
        }

        public static int GetTotalMinutes(this TimeSpan timespan)
        {
            return (int)timespan.TotalMinutes;
        }

        public static int GetTotalSeconds(this TimeSpan timespan)
        {
            return (int)timespan.TotalSeconds;
        }

        public static int GetTotalMilliseconds(this TimeSpan timespan)
        {
            return (int)timespan.TotalMilliseconds;
        }
    }
}
