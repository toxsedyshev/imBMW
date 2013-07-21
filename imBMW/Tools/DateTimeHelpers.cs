using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public static class DateTimeHelpers
    {
        public static int GetTotalHours(this TimeSpan timespan)
        {
            return timespan.Days * 24 + timespan.Hours;
        }

        public static int GetTotalMinutes(this TimeSpan timespan)
        {
            return timespan.GetTotalHours() * 60 + timespan.Minutes;
        }

        public static int GetTotalSeconds(this TimeSpan timespan)
        {
            return timespan.GetTotalMinutes() * 60 + timespan.Seconds;
        }

        public static int GetTotalMilliseconds(this TimeSpan timespan)
        {
            return timespan.GetTotalSeconds() * 1000 + timespan.Milliseconds;
        }
    }
}
