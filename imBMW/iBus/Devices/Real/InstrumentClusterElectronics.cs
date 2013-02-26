using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public enum IgnitionState
    {
        Off,
        On
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

    public delegate void IgnitionEventHandler(IgnitionEventArgs e);

    #endregion


    public static class InstrumentClusterElectronics
    {
        static byte[] DataIgnitionOff = new byte[] { 0x11, 0x00 };
        static byte[] DataIgnitionOn = new byte[] { 0x11, 0x01 };

        static IgnitionState currentIgnitionState = IgnitionState.Off;

        static InstrumentClusterElectronics()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, ProcessIKEMessage);
        }

        static void ProcessIKEMessage(Message m)
        {
            if (m.Data.Compare(DataIgnitionOn))
            {
                CurrentIgnitionState = IgnitionState.On;
            }
            else if (m.Data.Compare(DataIgnitionOff))
            {
                CurrentIgnitionState = IgnitionState.Off;
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

        public static event IgnitionEventHandler IgnitionStateChanged;
    }
}
