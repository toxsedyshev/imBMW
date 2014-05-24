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

        static readonly Message MessageOpenTrunk = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open trunk", 0x0C, 0x95, 0x01);

        static readonly Message MessageLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Lock doors", 0x0C, 0x4F, 0x01); // 0x0C, 0x97, 0x01
        static readonly Message MessageLockDriverDoor = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Lock driver door", 0x0C, 0x47, 0x01);
        static readonly Message MessageUnlockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unlock doors", 0x0C, 0x45, 0x01); // 0x0C, 0x03, 0x01

        //static Message MessageOpenWindows = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x00, 0x65);
        
        static readonly Message MessageOpenWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver front window", 0x0C, 0x52, 0x01);
        static readonly Message MessageOpenWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver rear window", 0x0C, 0x41, 0x01);
        static readonly Message MessageOpenWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger front window", 0x0C, 0x54, 0x01);
        static readonly Message MessageOpenWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger rear window", 0x0C, 0x44, 0x01);

        static readonly Message MessageCloseWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver front window", 0x0C, 0x53, 0x01);
        static readonly Message MessageCloseWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver rear window", 0x0C, 0x42, 0x01);
        static readonly Message MessageCloseWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger front window", 0x0C, 0x55, 0x01);
        static readonly Message MessageCloseWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger rear window", 0x0C, 0x43, 0x01);

        static readonly Message MessageOpenSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open sunroof", 0x0C, 0x7E, 0x01);
        static readonly Message MessageCloseSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close sunroof", 0x0C, 0x7F, 0x01);

        static readonly Message MessageFoldDriverMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Fold driver mirror", 0x0C, 0x01, 0x31, 0x01);
        static readonly Message MessageFoldPassengerMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Fold passenger mirror", 0x0C, 0x02, 0x31, 0x01);
        static readonly Message MessageUnfoldDriverMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unfold driver mirror", 0x0C, 0x01, 0x30, 0x01);
        static readonly Message MessageUnfoldPassengerMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unfold passenger mirror", 0x0C, 0x02, 0x30, 0x01);

        static readonly Message MessageFoldMirrorsE46 = new Message(DeviceAddress.MirrorMemorySecond, DeviceAddress.MirrorMemory, "Fold mirrors", 0x6D, 0x90);
        static readonly Message MessageUnfoldMirrorsE46 = new Message(DeviceAddress.MirrorMemorySecond, DeviceAddress.MirrorMemory, "Unfold mirrors", 0x6D, 0xA0);

        static readonly Message MessageGetAnalogValues = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Get analog values", 0x0B, 0x01);

        private static readonly Message MessageTurnOffAllExteriorLights = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Turn off all exterior lights", 0x76, 0x00);
        private static readonly Message MessageTurnOffInteriorLights = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Turn off all interior lights", 0x0C, 0x01, 0x01);

        private static readonly Message MessageTurnOnClownNose = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Turn on clown nose for 3 seconds", 0x0C, 0x4E, 0x01);
        private static readonly Message MessageTurnOnInterorLights = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Turn on interior lights for 3 seconds", 0x0C, 0x60, 0x01);

        private static readonly Message MessageFlashWarningLigths = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Flash warning lights", 0x76, 0x02);
        private static readonly Message MessageFlashLowbeams = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Flash lowbeams", 0x76, 0x04);
        private static readonly Message MessageFlashLowbeamsAndWarningLights = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Flash lowbeams and warning lights", 0x76, 0x06);
        private static readonly Message MessageFlashHighbeams = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Flash highbeams", 0x76, 0x08);
        private static readonly Message MessageFlashHighbeamsAndWarningLights = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Flash highbeams and warning lights", 0x76, 0x0A);
        private static readonly Message MessageFlashHighbeamsAndLowbeams = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Flash highbeams and lowbeams", 0x76, 0x0C);
        private static readonly Message MessageFlashHighbeamsAndLowbeamsAndWarningLights = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, "Flash highbeams, lowbeams and warning lights", 0x76, 0x0E);
        private static readonly Message MessageFlashDashBlinkers = new Message(DeviceAddress.LightControlModule, DeviceAddress.GlobalBroadcastAddress, "Flash dash blinkers", 0x5B, 0x60, 0x00, 0x04, 0x00);
        private static readonly Message MessageFlashDashBlinkersFastSlowIntermitent = new Message(DeviceAddress.LightControlModule, DeviceAddress.GlobalBroadcastAddress, "Flash dash blinkers fast/slow intermittent", 0x5B, 0x60, 0x00, 0x80, 0x00);

        private static readonly Message MessageCycleExteriorLightning = new Message(DeviceAddress.Diagnostic, DeviceAddress.GlobalBroadcastAddress, "Cycle through exterior lighting patterns", 0x0C, 0x01, 0x01);

        #endregion

        static double _batteryVoltage;

        static BodyModule()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.BodyModule, ProcessGMMessage);
        }

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
            get { return _batteryVoltage; }
            private set
            {
                // always notify to know that message was received
                /*if (batteryVoltage == value)
                {
                    return;
                }*/
                _batteryVoltage = value;

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
            Manager.EnqueueMessage(MessageLockDriverDoor);
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
