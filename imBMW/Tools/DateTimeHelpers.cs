using System;

namespace imBMW.Tools
{
    public static class DateTimeHelpers
    {
        public static int GetTotalHours(this TimeSpan timespan)
        {
            if (timespan.Days == 0)
            {
                return timespan.Hours;
            }
            return timespan.Days * 24 + timespan.Hours;
        }

        public static int GetTotalMinutes(this TimeSpan timespan)
        {
            if (timespan.GetTotalHours() == 0)
            {
                return timespan.Minutes;
            }
            return timespan.GetTotalHours() * 60 + timespan.Minutes;
        }

        public static int GetTotalSeconds(this TimeSpan timespan)
        {
            if (timespan.GetTotalMinutes() == 0)
            {
                return timespan.Seconds;
            }
            return timespan.GetTotalMinutes() * 60 + timespan.Seconds;
        }

        public static int GetTotalMilliseconds(this TimeSpan timespan)
        {
            if (timespan.GetTotalSeconds() == 0)
            {
                return timespan.Milliseconds;
            }
            return timespan.GetTotalSeconds() * 1000 + timespan.Milliseconds;
        }
    }
}
