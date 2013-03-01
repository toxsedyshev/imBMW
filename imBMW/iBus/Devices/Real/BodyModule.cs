using System;
using Microsoft.SPOT;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public enum RemoteKeyButton 
    {
        Lock, 
        Unlock,
        Trunk
    }

    public class RemoteKeyEventArgs : EventArgs
    {
        public RemoteKeyButton Button { get; private set; }

        public RemoteKeyEventArgs(RemoteKeyButton button)
        {
            Button = button;
        }
    }

    public delegate void RemoteKeyButtonEventHandler(RemoteKeyEventArgs e);

    #endregion


    public static class BodyModule
    {
        #region Messages

        static Message MessageOpenTrunk = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x95, 0x01);

        static Message MessageLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x97, 0x01);
        static Message MessageUnlockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x03, 0x01);

        static Message MessageOpenWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x52, 0x01);
        static Message MessageOpenWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x41, 0x01);
        static Message MessageOpenWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x54, 0x01);
        static Message MessageOpenWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x44, 0x01);

        static Message MessageCloseWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x53, 0x01);
        static Message MessageCloseWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x42, 0x01);
        static Message MessageCloseWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x55, 0x01);
        static Message MessageCloseWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x43, 0x01);

        static Message MessageOpenSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x7E, 0x01);
        static Message MessageCloseSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x7F, 0x01);

        #endregion

        static BodyModule()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.BodyModule, ProcessGMMessage);
        }

        static void ProcessGMMessage(Message m)
        {
            if (m.Data.Length == 2 && m.Data[0] == 0x72)
            {
                switch (m.Data[1])
                {
                    case 0x12:
                        OnRemoteKeyButton(RemoteKeyButton.Lock);
                        break;
                    case 0x22:
                        OnRemoteKeyButton(RemoteKeyButton.Unlock);
                        break;
                    case 0x42:
                        OnRemoteKeyButton(RemoteKeyButton.Trunk);
                        break;
                }
            }
        }

        static void OnRemoteKeyButton(RemoteKeyButton button)
        {
            var e = RemoteKeyButtonPressed;
            if (e != null)
            {
                e(new RemoteKeyEventArgs(button));
            }
        }

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

        public static void OpenWindows()
        {
            Manager.EnqueueMessage(MessageOpenWindowDriverFront);
            Manager.EnqueueMessage(MessageOpenWindowPassengerFront);
            Manager.EnqueueMessage(MessageOpenWindowPassengerRear);
            Manager.EnqueueMessage(MessageOpenWindowDriverRear);
        }

        public static void CloseWindows()
        {
            Manager.EnqueueMessage(MessageCloseWindowDriverFront);
            Manager.EnqueueMessage(MessageCloseWindowPassengerFront);
            Manager.EnqueueMessage(MessageCloseWindowPassengerRear);
            Manager.EnqueueMessage(MessageCloseWindowDriverRear);
        }

        public static void OpenSunroof()
        {
            Manager.EnqueueMessage(MessageOpenSunroof);
        }

        public static void CloseSunroof()
        {
            Manager.EnqueueMessage(MessageCloseSunroof);
        }

        public static event RemoteKeyButtonEventHandler RemoteKeyButtonPressed;
    }
}
