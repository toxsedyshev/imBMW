using System;
using Microsoft.SPOT;
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

    public class IgnitionEventArgs : EventArgs
    {
        public IgnitionState CurrentIgnitionState { get; private set; }
        public IgnitionState PreviousIgnitionState { get; private set; }

        public IgnitionEventArgs(IgnitionState current, IgnitionState previous)
        {
            CurrentIgnitionState = current;
            PreviousIgnitionState = previous;
        }
    }

    public class SpeedRPMArgs : EventArgs
    {
        public uint Speed { get; private set; }
        public uint RPM { get; private set; }

        public SpeedRPMArgs(uint speed, uint rpm)
        {
            Speed = speed;
            RPM = rpm;
        }
    }

    public delegate void IgnitionEventHandler(IgnitionEventArgs e);

    public delegate void SpeedRPMEventHandler(SpeedRPMArgs e);

    #endregion


    public static class InstrumentClusterElectronics
    {
        static byte[] DataIgnitionOff = new byte[] { 0x11, 0x00 };
        static byte[] DataIgnitionAcc = new byte[] { 0x11, 0x01 };
        static byte[] DataIgnitionIgn = new byte[] { 0x11, 0x04 }; // what is 0x02 ?

        static IgnitionState currentIgnitionState = IgnitionState.Off;

        public static uint CurrentRPM { get; private set; }
        public static uint CurrentSpeed { get; private set; }

        static InstrumentClusterElectronics()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, ProcessIKEMessage);
        }

        static void ProcessIKEMessage(Message m)
        {
            if (m.Data.Compare(DataIgnitionAcc))
            {
                CurrentIgnitionState = IgnitionState.Acc;
            }
            else if (m.Data.Compare(DataIgnitionIgn))
            {
                CurrentIgnitionState = IgnitionState.Ign;
            }
            else if (m.Data.Compare(DataIgnitionOff))
            {
                CurrentIgnitionState = IgnitionState.Off;
            }
            else if (m.Data.Length >= 3 && m.Data[0] == 0x18)
            {
                SpeedRPMData = m.Data;
            }
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
                Logger.Info("Ignition " + currentIgnitionState.ToStringValue());
            }
        }

        public static byte[] SpeedRPMData
        {
            set
            {
                CurrentSpeed = ((uint)value[1]) * 2;
                CurrentRPM = ((uint)value[2]) * 100;
                var e = SpeedRPMChanged;
                if (e != null)
                {
                    e(new SpeedRPMArgs(CurrentSpeed, CurrentRPM));
                }
            }
        }

        public static event IgnitionEventHandler IgnitionStateChanged;

        /// <summary>
        /// IKE sends speed and RPM every 2 sec
        /// </summary>
        public static event SpeedRPMEventHandler SpeedRPMChanged;
    }
}
