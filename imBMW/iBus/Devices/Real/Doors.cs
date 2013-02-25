using System;
using Microsoft.SPOT;

namespace imBMW.iBus.Devices.Real
{
    static class Doors
    {
        static Message MessageOpenTrunk = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x02, 0x01);

        public static void OpenTrunk()
        {
            Manager.EnqueueMessage(MessageOpenTrunk);
        }
    }
}
