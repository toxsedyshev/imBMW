using System;
using Microsoft.SPOT;
using System.IO;

namespace imBMW.Tools
{
    public class FileLogger
    {
        const int flushLines = 5;

        static int unflushed = 0;

        static StreamWriter writer;
        static QueueThreadWorker queue;
        static Action flushCallback;

        public static void Init(string path, Action flushCallback = null)
        {
            try
            {
                FileLogger.flushCallback = flushCallback;

                queue = new QueueThreadWorker(ProcessItem);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string fullpath;
                int i = 0;
                do
                {
                    fullpath = path + @"\imBMW" + (i++ == 0 ? "" : i.ToString()) + ".log";
                } while (File.Exists(fullpath));
                writer = new StreamWriter(fullpath);

                Logger.Logged += Logger_Logged;

                Logger.Info("File logger path: " + fullpath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "file logger init");
            }
        }

        static void Logger_Logged(LoggerArgs args)
        {
            queue.Enqueue(args.LogString);
        }

        static void ProcessItem(object o)
        {
            try
            {
                writer.WriteLine((string)o);
                o = null;
                if (++unflushed == flushLines)
                {
                    writer.Flush();
                    if (flushCallback != null)
                    {
                        flushCallback();
                    }
                    Debug.GC(true);
                    unflushed = 0;
                }
            }
            catch (Exception ex)
            {
                // don't use logger to prevent recursion
                Debug.Print("Can't write log to sd: " + ex.Message);
            }
        }
    }
}
