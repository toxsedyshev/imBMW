using System;
using Microsoft.SPOT;
using System.IO;

namespace imBMW.Tools
{
    public enum MenuMode : byte
    {
        RadioCDC = 1,
        BordmonitorAUX = 2,
        BordmonitorCDC = 3
    }

    public class Settings
    {
        static Settings instance;

        protected string settingsPath;
        
        public bool Log { get; set; }

        public bool LogToSD { get; set; }

        public bool LogMessageToASCII { get; set; }

        public bool MenuModeMK2 { get; set; }

        public MenuMode MenuMode { get; set; }

        public static Settings Init(string path)
        {
            Instance = new Settings();
            Instance.InitDefault();
            if (path != null && File.Exists(path))
            {
                Instance.InitFile(path);
            }
            else
            {
                Logger.Info("No settings file");
            }
            return Instance;
        }

        protected virtual void InitDefault()
        {
            Log = false;
            LogToSD = false;
            LogMessageToASCII = false;
            MenuModeMK2 = false;
            MenuMode = Tools.MenuMode.RadioCDC;
        }

        protected virtual void InitFile(string path)
        {
            try
            {
                using (var sr = new StreamReader(path))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s == string.Empty || s[0] == '#')
                        {
                            continue;
                        }
                        var parts = s.Split('=');
                        ProcessSetting(parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : null);
                    }
                }
                settingsPath = path;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "reading settings from file");
            }
        }

        protected virtual void ProcessSetting(string name, string value)
        {
            try
            {
                Logger.Info("Setting: " + name + " = " + (value ?? ""));
                bool isTrue = value == "1";
                switch (name)
                {
                    case "Log":
                        Log = isTrue;
                        break;
                    case "LogToSD":
                        LogToSD = isTrue;
                        break;
                    case "LogMessageToASCII":
                        LogMessageToASCII = isTrue;
                        break;
                    case "MenuModeMK2":
                        MenuModeMK2 = isTrue;
                        break;
                    case "MenuMode":
                        MenuMode = (Tools.MenuMode)byte.Parse(value);
                        break;
                    default:
                        Logger.Warning("  Unknown setting");
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "reading setting");
            }
        }

        public static Settings Instance
        {
            protected set
            {
                instance = value;
            }
            get
            {
                if (instance == null)
                {
                    Init(null);
                }
                return instance;
            }
        }
    }
}
