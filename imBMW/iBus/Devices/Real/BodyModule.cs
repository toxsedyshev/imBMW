using System;
using Microsoft.SPOT;
using imBMW.Tools;

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

    public delegate void VoltageEventHandler(double voltage);

    #endregion


    public static class BodyModule
    {
        #region Messages

        static Message MessageOpenTrunk = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open trunk", 0x0C, 0x95, 0x01);

        static Message MessageLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Lock doors", 0x0C, 0x4F, 0x01); // 0x0C, 0x97, 0x01
        static Message MessageUnlockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unlock doors", 0x0C, 0x45, 0x01); // 0x0C, 0x03, 0x01

        //static Message MessageOpenWindows = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x00, 0x65);
        
        static Message MessageOpenWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver front window", 0x0C, 0x52, 0x01);
        static Message MessageOpenWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver rear window", 0x0C, 0x41, 0x01);
        static Message MessageOpenWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger front window", 0x0C, 0x54, 0x01);
        static Message MessageOpenWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger rear window", 0x0C, 0x44, 0x01);

        static Message MessageCloseWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver front window", 0x0C, 0x53, 0x01);
        static Message MessageCloseWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver rear window", 0x0C, 0x42, 0x01);
        static Message MessageCloseWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger front window", 0x0C, 0x55, 0x01);
        static Message MessageCloseWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger rear window", 0x0C, 0x43, 0x01);

        static Message MessageOpenSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open sunroof", 0x0C, 0x7E, 0x01);
        static Message MessageCloseSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close sunroof", 0x0C, 0x7F, 0x01);

        static Message MessageFoldDriverMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Fold driver mirror", 0x0C, 0x01, 0x31, 0x01);
        static Message MessageFoldPassengerMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Fold passenger mirror", 0x0C, 0x02, 0x31, 0x01);
        static Message MessageUnfoldDriverMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unfold driver mirror", 0x0C, 0x01, 0x30, 0x01);
        static Message MessageUnfoldPassengerMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unfold passenger mirror", 0x0C, 0x02, 0x30, 0x01);

        static Message MessageFoldMirrorsE46 = new Message(DeviceAddress.MirrorMemorySecond, DeviceAddress.MirrorMemory, "Fold mirrors", 0x6D, 0x90);
        static Message MessageUnfoldMirrorsE46 = new Message(DeviceAddress.MirrorMemorySecond, DeviceAddress.MirrorMemory, "Unfold mirrors", 0x6D, 0xA0);

        static Message MessageGetAnalogValues = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Get analog values", 0x0B, 0x01);

        #endregion

        static double batteryVoltage;

        static BodyModule()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.BodyModule, ProcessGMMessage);
        }

        static void ProcessGMMessage(Message m)
        {
            if (m.Data.Length == 2 && m.Data[0] == 0x72)
            {
                var btn = m.Data[1];
                switch (btn)
                {
                    case 0x12: // TODO maybe it contains key number?
                    case 0x16:
                        OnRemoteKeyButton(m, RemoteKeyButton.Lock);
                        break;
                    case 0x22:
                    case 0x26:
                        OnRemoteKeyButton(m, RemoteKeyButton.Unlock);
                        break;
                    case 0x42:
                    case 0x46:
                        OnRemoteKeyButton(m, RemoteKeyButton.Trunk);
                        break;
                    default:
                        m.ReceiverDescription = "Remote key unknown button " + btn.ToHex() + " press";
                        break;
                }
            }
            else if (m.Data.Length > 3 && m.Data[0] == 0xA0)
            {
                var voltage = ((double)m.Data[1]) / 10 + ((double)m.Data[2]) / 1000;

                m.ReceiverDescription = "Analog values. Battery voltage = " + voltage + "V";
                BatteryVoltage = voltage;
            }
        }

        static void OnRemoteKeyButton(Message m, RemoteKeyButton button)
        {
            var e = RemoteKeyButtonPressed;
            if (e != null)
            {
                e(new RemoteKeyEventArgs(button));
            }
            m.ReceiverDescription = "Remote key press " + button.ToStringValue() + " button";
            Logger.Info(m.ReceiverDescription);
        }

        public static double BatteryVoltage
        {
            get { return batteryVoltage; }
            private set
            {
                // always notify to know that message was received
                /*if (batteryVoltage == value)
                {
                    return;
                }*/
                batteryVoltage = value;

                var e = BatteryVoltageChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        public static void UpdateBatteryVoltage()
        {
            Manager.EnqueueMessage(MessageGetAnalogValues);
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

        /// <summary>
        /// Warning! Opens windows just by half!
        /// </summary>
        public static void OpenWindows()
        {
            Manager.EnqueueMessage(MessageOpenWindowDriverFront, 
                MessageOpenWindowPassengerFront, 
                MessageOpenWindowPassengerRear,
                MessageOpenWindowDriverRear);
        }

        /// <summary>
        /// Warning! Closes windows just by half!
        /// </summary>
        public static void CloseWindows()
        {
            Manager.EnqueueMessage(MessageCloseWindowDriverFront,
                MessageCloseWindowPassengerFront,
                MessageCloseWindowPassengerRear,
                MessageCloseWindowDriverRear);
        }

        public static void OpenSunroof()
        {
            Manager.EnqueueMessage(MessageOpenSunroof);
        }

        public static void CloseSunroof()
        {
            Manager.EnqueueMessage(MessageCloseSunroof);
        }

        /// <summary>
        /// Now only E39 mirrors are supported, E46 not tested
        /// </summary>
        public static void FoldMirrors()
        {
            Manager.EnqueueMessage(MessageFoldMirrorsE46,
                MessageFoldPassengerMirrorE39,
                MessageFoldDriverMirrorE39);
        }

        /// <summary>
        /// Now only E39 mirrors are supported, E46 not tested
        /// </summary>
        public static void UnfoldMirrors()
        {
            Manager.EnqueueMessage(MessageUnfoldMirrorsE46,
                MessageUnfoldPassengerMirrorE39,
                MessageUnfoldDriverMirrorE39);
        }

        public static event RemoteKeyButtonEventHandler RemoteKeyButtonPressed;

        public static event VoltageEventHandler BatteryVoltageChanged;
    }
}
