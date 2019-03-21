//#define CANBUS
#if CANBUS
#define E65SEATS
#endif

//#define MenuRadioCDC
//#define MenuBordmonitorAUX
//#define MenuBordmonitorCDC
//#define MenuMIDAUX

using imBMW.Devices.V2.Hardware;
using imBMW.Tools;

namespace imBMW.Devices.V2
{
    public class Program
    {
        public static void Main()
        {
            var launcher = new Launcher(new LauncherSettings
            {
                HWVersion = "HW2",
                LEDPin = Pin.LED,
                iBusPort = Pin.TH3122Port,
                iBusBusyPin = Pin.TH3122SENSTA,
                MediaShieldPort = Pin.Com2,
                MediaShieldLED = Pin.Di10,
                SDInterface = Pin.SDInterface,
#if CANBUS
                CanBus = Pin.CAN1,
#endif
#if E65SEATS
                E65Seats = true,
#endif
            }, SettingsOverride);

            launcher.Run();
        }

        private static void SettingsOverride(Settings settings)
        {
#if MenuRadioCDC
            settings.MenuMode = MenuMode.RadioCDC;
#elif MenuBordmonitorAUX
            settings.MenuMode = MenuMode.BordmonitorAUX;
#elif MenuBordmonitorCDC
            settings.MenuMode = MenuMode.BordmonitorCDC;
#elif MenuMIDAUX
            settings.MenuMode = MenuMode.MIDAUX;
#endif
        }
    }
}
