using System;
using Microsoft.SPOT;

namespace imBMW.iBus.Devices.Real
{
    public static class Doors
    {
        static Message MessageOpenTrunk = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x95, 0x01);
        static Message MessageLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x97, 0x01);
        static Message MessageUnlockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x03, 0x01);

        public static void OpenTrunk()
        {
            Manager.EnqueueMessage(MessageOpenTrunk);
        }

        public static void LockDoors()
        {
            Manager.EnqueueMessage(MessageLockDoors);
        }

        public static void UnlockDoors()
        {
            Manager.EnqueueMessage(MessageUnlockDoors);
        }
    }
}
