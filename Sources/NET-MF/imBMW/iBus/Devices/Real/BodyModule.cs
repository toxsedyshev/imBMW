using System;
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

    public class RemoteKeyEventArgs
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

    /// <summary>
    /// Body Module class. Aka General Module 5 (GM5) or ZKE5.
    /// </summary>
    public static class BodyModule
    {
        #region Messages

        static Message MessageOpenTrunk = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open trunk", 0x0C, 0x95, 0x01);

        static Message MessageLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Lock doors", 0x0C, 0x4F, 0x01); // 0x0C, 0x97, 0x01
        static Message MessageLockDriverDoor = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Lock driver door", 0x0C, 0x47, 0x01);
        static Message MessageUnlockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unlock doors", 0x0C, 0x45, 0x01); // 0x0C, 0x03, 0x01
        static Message MessageToggleLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Toggle lock doors", 0x0C, 0x03, 0x01); // TODO after it sometimes can't open usign hardware button
        static Message MessageRequestDoorsStatus = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.BodyModule, "Request doors status", 0x79);

        //static Message MessageOpenWindows = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x00, 0x65);
        
        public static Message MessageOpenWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver front window", 0x0C, 0x52, 0x01);
        public static Message MessageOpenWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver rear window", 0x0C, 0x41, 0x01);
        public static Message MessageOpenWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger front window", 0x0C, 0x54, 0x01);
        public static Message MessageOpenWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger rear window", 0x0C, 0x44, 0x01);

        public static Message MessageCloseWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver front window", 0x0C, 0x53, 0x01);
        public static Message MessageCloseWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver rear window", 0x0C, 0x42, 0x01);
        public static Message MessageCloseWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger front window", 0x0C, 0x55, 0x01);
        public static Message MessageCloseWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger rear window", 0x0C, 0x43, 0x01);

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
        static bool isCarLocked;
        static bool wasDriverDoorOpened;

        static BodyModule()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.BodyModule, ProcessGMMessage);
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessGMMessage(Message m)
        {
            if (m.Data.Length == 2 && m.Data[0] == 0x72)
            {
                var btn = m.Data[1];
                if (btn.HasBit(4)) // 0x1_
                {
                    OnRemoteKeyButton(m, RemoteKeyButton.Lock);
                }
                else if (btn.HasBit(5)) // 0x2_
                {
                    OnRemoteKeyButton(m, RemoteKeyButton.Unlock);
                }
                else if (btn.HasBit(6)) // 0x4_
                {
                    OnRemoteKeyButton(m, RemoteKeyButton.Trunk);
                }
            }
            else if (m.Data.Length == 3 && m.Data[0] == 0x7A)
            {
                // Data[1] = 7654 3210. 7 = ??, 6 = light, 5 = lock, 4 = unlock, 5+4 = hard lock,
                //      doors statuses: 0 = left front (driver), 1 = right front, 2 = left rear, 3 = right rear.
                // Car could have locked status even after doors are opened!
                // Data[2] = 7654 3210. 5 = trunk.
                isCarLocked = m.Data[1].HasBit(5);
                if (isCarLocked)
                {
                    if (m.Data[1].HasBit(0))
                    {
                        wasDriverDoorOpened = true;
                    }
                }
                else
                {
                    wasDriverDoorOpened = false;
                }
            }
            else if (m.Data.Length > 3 && m.Data[0] == 0xA0)
            {
                // TODO filter not analog-values responses
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

        public static bool IsCarLocked
        {
            get { return isCarLocked; }
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
            if (!isCarLocked || wasDriverDoorOpened)
            {
                isCarLocked = true;
                wasDriverDoorOpened = false;
                Manager.EnqueueMessage(MessageToggleLockDoors, MessageRequestDoorsStatus);
            }
        }

        public static bool UnlockDoors()
        {
            if (!isCarLocked || wasDriverDoorOpened)
            {
                return !isCarLocked;
            }
            isCarLocked = wasDriverDoorOpened;
            wasDriverDoorOpened = false;
            Manager.EnqueueMessage(MessageToggleLockDoors, MessageRequestDoorsStatus);
            return !isCarLocked;
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
