using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW
{
    public class Logger
    {
        public static void Error(Exception ex, string message)
        {
            Debug.WriteLine(DateTime.Now.ToString() + " [ERR] " + message + ": " + ex.Message + " Stack trace:\n" + ex.StackTrace);
        }

        public static void Info(string message, string priority = null)
        {
            if (priority == null)
            {
                priority = "i";
            }
            Debug.WriteLine(DateTime.Now.ToString() + " [" + priority + "] " + message);
        }
    }
}
