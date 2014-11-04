using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.Features.Localizations
{
    public abstract class Localization
    {
        protected static Localization current;

        public static Localization Current
        {
            get
            {
                if (current == null)
                {
                    current = new EnglishLocalization();
                } 
                return Localization.current;
            }
            set { Localization.current = value; }
        }

        #region Keys

        public abstract string LanguageName { get; }

        public abstract string Language { get; }

        public abstract string Settings { get; }
        
        public abstract string Bordcomputer { get; }

        public abstract string BordcomputerShort { get; }

        public abstract string Speed { get; }

        public abstract string KMH { get; }
        
        public abstract string Revs { get; }

        public abstract string Voltage { get; }

        public abstract string VoltageShort { get; }

        public abstract string Engine { get; }

        public abstract string Outside { get; }
        
        public abstract string Refreshing { get; }
        
        public abstract string Player { get; }

        public abstract string Phone { get; }

        public abstract string QuickAccess { get; }
        
        public abstract string ComfortWindows { get; }

        public abstract string ComfortSunroof { get; }

        public abstract string AutoLock { get; }

        public abstract string AutoUnlock { get; }
        
        public abstract string VoiceCall { get; }

        public abstract string Contacts { get; }
        
        public abstract string PrevItems { get; }

        public abstract string NextItems { get; }

        public abstract string Back { get; }
        
        public abstract string Volume { get; }
        
        public abstract string Reconnect { get; }

        public abstract string Pair { get; }

        public abstract string Playing { get; }

        public abstract string Paused { get; }

        public abstract string Play { get; }
        
        public abstract string Pause { get; }
        
        public abstract string PrevTrack { get; }

        public abstract string NextTrack { get; }
        
        public abstract string Connected { get; }

        public abstract string Waiting { get; }

        public abstract string Disconnected { get; }
        
        public abstract string NotPaired { get; }
        
        public abstract string Next { get; }

        public abstract string Previous { get; }
        
        public abstract string NoContacts { get; }

        public virtual string DegreeCelsius { get { return "°C"; } }
        
        public abstract string Disconnect { get; }

        /*
        
        public abstract string  { get; }

         */

        #endregion

        public static void SetCurrent(string language)
        {
            Current = Get(language);
        }

        public static Localization Get(string language)
        {
            if (StringHelpers.IsNullOrEmpty(language))
            {
                language = EnglishLocalization.SystemName;
            }
            switch (language)
            {
                case EnglishLocalization.SystemName:
                    return new EnglishLocalization();
                case RussianLocalization.SystemName:
                    return new RussianLocalization();
                case RadioLocalization.SystemName:
                    return new RadioLocalization();
                default:
                    throw new Exception("Wrong localization name \"" + language + "\"");
            }
        }
    }
}
