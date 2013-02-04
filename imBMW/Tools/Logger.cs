using System;
using Microsoft.SPOT;
using imBMW.iBus;

namespace imBMW.Tools
{
    #region Enums, delegates and event args

    enum LogPriority
    {
        Info = 3,
        Warning = 2,
        Error = 1
    }

    class LoggerArgs 
    {
        public readonly DateTime Timestamp;
        public readonly String Message;
        public readonly String LogString;
        public readonly LogPriority Priority;
        public readonly Exception Exception;
        public readonly iBus.Message iBusMessage;

        public LoggerArgs(LogPriority priority, string message)
        {
            Timestamp = DateTime.Now;
            Priority = priority;
            Message = message;
            switch (priority)
            {
                case LogPriority.Error:
                    LogString = Timestamp + " [ERROR] ";
                    break;
                case LogPriority.Warning:
                    LogString = Timestamp + " [warn] ";
                    break;
                case LogPriority.Info:
                    LogString = Timestamp + " [i] ";
                    break;
            }
            LogString += message;
        }
    }

    delegate void LoggerEventHangler(LoggerArgs args);

    #endregion

    static class Logger
    {
        public static event LoggerEventHangler Logged;

        public static void Log(LogPriority priority, string message)
        {
            var e = Logged;
            if (e != null)
            {
                e(new LoggerArgs(priority, message));
            }
        }

        public static void Log(LogPriority priority, Exception exception, string message = null)
        {
            if (Logged == null)
            {
                return;
            }
            message = exception.Message + (message != null ? " (" + message + ")" : String.Empty) + ". Stack trace: \n" + exception.StackTrace;
            Log(priority, message);
        }

        public static void Info(string message)
        {
            Log(LogPriority.Info, message);
        }

        public static void Info(iBus.Message message)
        {
            Log(LogPriority.Info, message.ToPrettyString());
        }

        public static void Warning(string message)
        {
            Log(LogPriority.Warning, message);
        }

        public static void Warning(iBus.Message message)
        {
            Log(LogPriority.Warning, message.ToPrettyString());
        }

        public static void Warning(Exception exception, string message = null)
        {
            Log(LogPriority.Warning, exception, message);
        }

        public static void Error(string message)
        {
            Log(LogPriority.Error, message);
        }

        public static void Error(Exception exception, string message = null)
        {
            Log(LogPriority.Error, exception, message);
        }
    }
}
