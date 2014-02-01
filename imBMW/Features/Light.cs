using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.iBus;

namespace imBMW.Features
{
    public static class Light
    {
        /// <summary>
        /// Warning! Error will blink on the IKE on each light status change.
        /// </summary>
        public static bool IgnoreFrontLightsError = false;

        static Light()
        {
            LightControlModule.LightStatusReceived += LightControlModule_LightStatusReceived;
        }

        static void LightControlModule_LightStatusReceived(iBus.Message message, LightStatusEventArgs args)
        {
            if (IgnoreFrontLightsError && args.ErrorFrontLeftLights && args.ErrorFrontRightsLights)
            {
                var data = (byte[])message.Data.Clone();
                data[4] = data[4].RemoveBits(0x30);
                var m = new Message(DeviceAddress.LightControlModule, DeviceAddress.GlobalBroadcastAddress, data);
                Manager.EnqueueMessage(m);
            }
        }
    }
}
