
using imBMW.Tools;

namespace imBMW.Features.Localizations
{
    public class RadioLocalization : EnglishLocalization
    {
        public new const string SystemName = "Radio";

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

        public override string DegreeCelsius
        {
            get { return CharIcons.Degree + "C"; }
        }

        public override string Consumption
        {
            get { return "Consump."; }
        }

        public override string LimitCurrentSpeed
        {
            get { return "Curr. speed"; }
        }
    }
}
