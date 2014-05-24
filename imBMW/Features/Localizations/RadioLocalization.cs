using System;
using Microsoft.SPOT;

namespace imBMW.Features.Localizations
{
    public class RadioLocalization : EnglishLocalization
    {
        public override string PrevTrack
        {
            get { return "Prev track"; }
        }

        public override string Disconnected
        {
            get { return "Disconn"; }
        }

        public override string ComfortSunroof
        {
            get { return "Comf sunroof"; }
        }

        public override string ComfortWindows
        {
            get { return "Comf winds"; }
        }

        public override string QuickAccess
        {
            get { return "Quick acc"; }
        }

        public override string Bordcomputer
        {
            get { return "BC"; }
        }
    }
}
