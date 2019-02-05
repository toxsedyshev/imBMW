//#define CANBUS
#if CANBUS
#define E65SEATS
#endif

//#define MenuRadioCDC
//#define MenuBordmonitorAUX
//#define MenuBordmonitorCDC
//#define MenuMIDAUX

using GHI.IO.Storage;
using imBMW.Devices.V2.Hardware;
using System.IO.Ports;
using imBMW.Tools;

namespace imBMW.Devices.V2
{
    public class Program
    {
        public static void Main()
        {
            var launcher = new Launcher(new LauncherSettings
            {
                LEDPin = Pin.LED,
                iBusPort = Serial.COM3,
                iBusBusyPin = Pin.TH3122SENSTA,
                MediaShieldLED = Pin.Di10,
                MediaSheildPort = Serial.COM2,
                SDInterface = SDCard.SDInterface.MCI,
                HWVersion = "HW2",
#if CANBUS
                CanBus = GHI.IO.ControllerAreaNetwork.Channel.One,
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
