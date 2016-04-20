using System;
using Microsoft.SPOT;
using imBMW.iBus;

namespace imBMW.Tools
{
    public static class Logger
    {
        public static event LoggerEventHangler Logged;

        public static void Log(LogPriority priority, string message, string priorityTitle = null)
        {
            var e = Logged;
            if (e != null)
            {
                e(new LoggerArgs(priority, message, priorityTitle));
            }
        }

        public static void Log(LogPriority priority, Exception exception, string message = null, string priorityTitle = null)
        {
            if (Logged == null)
            {
                return;
            }
            message = exception.Message + (message != null ? " (" + message + ")" : String.Empty) + ". Stack trace: \n" + exception.StackTrace;
            Log(priority, message, priorityTitle);
        }

        public static void Info(string message, string priorityTitle = null)
        {
            Log(LogPriority.Info, message, priorityTitle);
        }

        public static void Info(iBus.Message message, string priorityTitle = null)
        {
            Log(LogPriority.Info, message.ToPrettyString(true), priorityTitle);
        }

        public static void Warning(string message, string priorityTitle = null)
        {
            Log(LogPriority.Warning, message, priorityTitle);
        }

        public static void Warning(iBus.Message message, string priorityTitle = null)
        {
            Log(LogPriority.Warning, message.ToPrettyString(true), priorityTitle);
        }

        public static void Warning(Exception exception, string message = null, string priorityTitle = null)
        {
            Log(LogPriority.Warning, exception, message, priorityTitle);
        }

        public static void Error(string message, string priorityTitle = null)
        {
            Log(LogPriority.Error, message, priorityTitle);
        }

        public static void Error(Exception exception, string message = null, string priorityTitle = null)
        {
            Log(LogPriority.Error, exception, message, priorityTitle);
        }
    }
}
