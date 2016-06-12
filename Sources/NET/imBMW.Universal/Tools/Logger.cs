using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW
{
    public enum LogPriority
    {
        Info, 
        Debug,
        Warning,
        Error
    }

    public class LogItem
    {
        public DateTime Timestamp { get; set; }

        public LogPriority Priority { get; set; }

        public string Message { get; set; }

        public string PriorityLabel { get; set; }

        public Exception Exception { get; set; }

        public LogItem()
        {
            Timestamp = DateTime.Now;
        }
    }

    public class Logger
    {
        public static event Action<LogItem> Logged;

        public static void Error(Exception ex, string message)
        {
            OnLogged(new LogItem
            {
                Priority = LogPriority.Error,
                PriorityLabel = "ERR",
                Message = message,
                Exception = ex
            });
        }

        public static void Info(string message, string priority = null)
        {
            if (priority == null)
            {
                priority = "i";
            }
            OnLogged(new LogItem
            {
                Priority = LogPriority.Info,
                PriorityLabel = "i",
                Message = message
            });
        }

        static void OnLogged(LogItem item)
        {
            var e = Logged;
            if (e != null)
            {
                e(item);
            }
        }
    }
}
