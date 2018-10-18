using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public static class DateTimeHelpers
    {
        public static int GetTotalHours(this TimeSpan timespan)
        {
            var days = timespan.Days;
            if (days == 0)
            {
                return timespan.Hours;
            }
            return days * 24 + timespan.Hours;
        }

        public static int GetTotalMinutes(this TimeSpan timespan)
        {
            var hours = timespan.GetTotalHours();
            if (hours == 0)
            {
                return timespan.Minutes;
            }
            return hours * 60 + timespan.Minutes;
        }

        public static long GetTotalSeconds(this TimeSpan timespan)
        {
            var minutes = timespan.GetTotalMinutes();
            if (minutes == 0)
            {
                return timespan.Seconds;
            }
            return (long)minutes * 60 + timespan.Seconds;
        }

        public static long GetTotalMilliseconds(this TimeSpan timespan)
        {
            var seconds = timespan.GetTotalSeconds();
            if (seconds == 0)
            {
                return timespan.Milliseconds;
            }
            return seconds * 1000 + timespan.Milliseconds;
        }
    }
}
