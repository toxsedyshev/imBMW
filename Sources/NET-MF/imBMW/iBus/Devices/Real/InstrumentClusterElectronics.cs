using System;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public enum IgnitionState
    {
        Off,
        Acc,
        Ign
    }

    public class IgnitionEventArgs
    {
        public IgnitionState CurrentIgnitionState { get; private set; }
        public IgnitionState PreviousIgnitionState { get; private set; }

        public IgnitionEventArgs(IgnitionState current, IgnitionState previous)
        {
            CurrentIgnitionState = current;
            PreviousIgnitionState = previous;
        }
    }

    public class SpeedRPMEventArgs
    {
        public uint Speed { get; private set; }
        public uint RPM { get; private set; }

        public SpeedRPMEventArgs(uint speed, uint rpm)
        {
            Speed = speed;
            RPM = rpm;
        }
    }

    public class TemperatureEventArgs
    {
        public sbyte Outside { get; private set; }
        public sbyte Coolant { get; private set; }

        public TemperatureEventArgs(sbyte outside, sbyte coolant)
        {
            Outside = outside;
            Coolant = coolant;
        }
    }

    public delegate void IgnitionEventHandler(IgnitionEventArgs e);

    public delegate void SpeedRPMEventHandler(SpeedRPMEventArgs e);

    public delegate void TemperatureEventHandler(TemperatureEventArgs e);

    #endregion


    public static class InstrumentClusterElectronics
    {
        static IgnitionState currentIgnitionState = IgnitionState.Off;

        public static uint CurrentRPM { get; private set; }
        public static uint CurrentSpeed { get; private set; }

        public static sbyte TemperatureOutside { get; private set; }
        public static sbyte TemperatureCoolant { get; private set; }

        static Message MessageGong1 = new Message(DeviceAddress.Radio, DeviceAddress.InstrumentClusterElectronics, "Gong 1", 0x23, 0x62, 0x30, 0x37, 0x08);
        static Message MessageGong2 = new Message(DeviceAddress.Radio, DeviceAddress.InstrumentClusterElectronics, "Gong 2", 0x23, 0x62, 0x30, 0x37, 0x10);

        static InstrumentClusterElectronics()
        {
            TemperatureOutside = sbyte.MinValue;
            TemperatureCoolant = sbyte.MinValue;

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, ProcessIKEMessage);
        }

        static void ProcessIKEMessage(Message m)
        {
            if (m.Data.Length == 3 && m.Data[0] == 0x18)
            {
                OnSpeedRPMChanged(((uint)m.Data[1]) * 2, ((uint)m.Data[2]) * 100);
                m.ReceiverDescription = "Speed " + CurrentSpeed + "km/h " + CurrentRPM + "RPM";
            }
            else if (m.Data.Length == 2 && m.Data[0] == 0x11)
            {
                byte ign = m.Data[1];
                if ((ign & 0x02) != 0)
                {
                    CurrentIgnitionState = IgnitionState.Ign;
                }
                else if ((ign & 0x01) != 0)
                {
                    CurrentIgnitionState = IgnitionState.Acc;
                }
                else if (ign == 0x00)
                {
                    CurrentIgnitionState = IgnitionState.Off;
                } 
                else
                {
                    m.ReceiverDescription = "Ignition unknown " + ign.ToHex();
                    return;
                }
                m.ReceiverDescription = "Ignition " + CurrentIgnitionState.ToStringValue();
            }
            else if (m.Data.Length == 4 && m.Data[0] == 0x19)
            {
                OnTemperatureChanged((sbyte)m.Data[1], (sbyte)m.Data[2]);
                m.ReceiverDescription = "Temperature. Outside " + TemperatureOutside + "°C, Coolant " + TemperatureCoolant + "°C";
            }
        }

        public static void Gong1()
        {
            Manager.EnqueueMessage(MessageGong1);
        }

        public static void Gong2()
        {
            Manager.EnqueueMessage(MessageGong2);
        }

        public static IgnitionState CurrentIgnitionState
        {
            get
            {
                return currentIgnitionState;
            }
            private set
            {
                if (currentIgnitionState == value)
                {
                    return;
                }
                var previous = currentIgnitionState;
                currentIgnitionState = value;
                var e = IgnitionStateChanged;
                if (e != null)
                {
                    e(new IgnitionEventArgs(currentIgnitionState, previous));
                }
                if (currentIgnitionState != IgnitionState.Ign)
                {
                    OnSpeedRPMChanged(CurrentSpeed, 0);
                }
                Logger.Info("Ignition " + currentIgnitionState.ToStringValue());
            }
        }

        private static void OnTemperatureChanged(sbyte outside, sbyte coolant)
        {
            TemperatureOutside = outside;
            TemperatureCoolant = coolant;
            var e = TemperatureChanged;
            if (e != null)
            {
                e(new TemperatureEventArgs(outside, coolant));
            }
        }

        private static void OnSpeedRPMChanged(uint speed, uint rpm)
        {
            CurrentSpeed = speed;
            CurrentRPM = rpm;
            var e = SpeedRPMChanged;
            if (e != null)
            {
                e(new SpeedRPMEventArgs(CurrentSpeed, CurrentRPM));
            }
        }

        public static event IgnitionEventHandler IgnitionStateChanged;

        /// <summary>
        /// IKE sends speed and RPM every 2 sec
        /// </summary>
        public static event SpeedRPMEventHandler SpeedRPMChanged;

        /// <summary>
        /// IKE sends temperature every TBD sec
        /// </summary>
        public static event TemperatureEventHandler TemperatureChanged;
    }
}
