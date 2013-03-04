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

    public class SpeedRPMEventArgs : EventArgs
    {
        public uint Speed { get; private set; }
        public uint RPM { get; private set; }

        public SpeedRPMEventArgs(uint speed, uint rpm)
        {
            Speed = speed;
            RPM = rpm;
        }
    }

    public delegate void IgnitionEventHandler(IgnitionEventArgs e);

    public delegate void SpeedRPMEventHandler(SpeedRPMEventArgs e);

    #endregion


    public static class InstrumentClusterElectronics
    {
        static IgnitionState currentIgnitionState = IgnitionState.Off;

        public static uint CurrentRPM { get; private set; }
        public static uint CurrentSpeed { get; private set; }

        static InstrumentClusterElectronics()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, ProcessIKEMessage);
        }

        static void ProcessIKEMessage(Message m)
        {
            if (m.Data.Length == 3 && m.Data[0] == 0x18)
            {
                SpeedRPMData = m.Data;
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
                    // TODO delete
                    Logger.Warning("Unknown ignition state " + ign.ToHex());
                }
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
                    e(new SpeedRPMEventArgs(CurrentSpeed, CurrentRPM));
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
