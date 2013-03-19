using System;
using Microsoft.SPOT;
using imBMW.iBus;

namespace imBMW.Tools
{
    #region Enums, delegates and event args

    public enum LogPriority
    {
        Info = 3,
        Warning = 2,
        Error = 1
    }

    public class LoggerArgs 
    {
        public readonly DateTime Timestamp;
        public readonly String Message;
        public readonly String LogString;
        public readonly LogPriority Priority;
        public readonly Exception Exception;
        public readonly iBus.Message iBusMessage;
        public readonly String PriorityTitle;

        public LoggerArgs(LogPriority priority, string message, string priorityTitle = null)
        {
            Timestamp = DateTime.Now;
            Priority = priority;
            Message = message;
            if (priorityTitle != null)
            {
                PriorityTitle = priorityTitle;
            } else {
                switch (priority)
                {
                    case LogPriority.Error:
                        PriorityTitle ="ERROR";
                        break;
                    case LogPriority.Warning:
                        PriorityTitle = "warn";
                        break;
                    case LogPriority.Info:
                        PriorityTitle = "i";
                        break;
                }
            }
            LogString = Timestamp.ToString("yy-MM-dd HH:mm:ss.fff") + " [" + PriorityTitle + "] " + message;
        }
    }

    public delegate void LoggerEventHangler(LoggerArgs args);

    #endregion

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
            Log(LogPriority.Info, message.ToPrettyString(), priorityTitle);
        }

        public static void Warning(string message, string priorityTitle = null)
        {
            Log(LogPriority.Warning, message, priorityTitle);
        }

        public static void Warning(iBus.Message message, string priorityTitle = null)
        {
            Log(LogPriority.Warning, message.ToPrettyString(), priorityTitle);
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
