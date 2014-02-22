using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    public static class InstrumentClusterElectronics
    {
        #region Public static fields

        public static event IgnitionEventHandler IgnitionStateChanged;

        public static event CarDataEventHandler CarDataChanged;

        #endregion

        #region Public static properties

        public static string VIN { get; private set; }

        public static short RPM { get; private set; }
        public static short Speed { get; private set; }
        public static uint Odometer { get; private set; }

        public static float Consumption1 { get; private set; }
        public static float Consumption2 { get; private set; }

        public static short Range { get; private set; }
        public static float AverageSpeed { get; private set; }

        public static float OutsideTemperature { get; private set; }
        public static float CoolantTemperature { get; private set; }

        public static IgnitionState CurrentIgnitionState
        {
            get
            {
                return _currentIgnitionState;
            }
            private set
            {
                if (_currentIgnitionState == value)
                {
                    return;
                }
                var previous = _currentIgnitionState;
                _currentIgnitionState = value;
                var e = IgnitionStateChanged;
                if (e != null)
                {
                    e(new IgnitionEventArgs(_currentIgnitionState, previous));
                }
                if (_currentIgnitionState != IgnitionState.Ign)
                {
                    OnCarDataChanged();
                }
                Logger.Info("Ignition " + _currentIgnitionState.ToStringValue());
            }
        }

        #endregion

        #region Private static fields
        
        private static IgnitionState _currentIgnitionState = IgnitionState.Off;
        private static bool _timeIsSet;
        private static bool _dateIsSet;

        #endregion

        #region Static constructor
        
        static InstrumentClusterElectronics()
        {
            Manager.AddMessageReceiverForSourceOrDestinationDevice(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.InstrumentClusterElectronics, ProcessMessage);
        }

        #endregion

        #region Private static methods

        private static bool ParseFloat(byte[] data, out float result)
        {
            result = 0;
            var str = string.Empty;
            for (int i = 3; i < data.Length; i++)
            {
                str += (char)(data[i]);
            }
            double d;
            if (Parse.TryParseDouble(str, out d))
            {
                result = (float) d;
                return true;
            }
            return false;
        }

        private static bool ParseInt(byte[] data, out int result)
        {
            result = 0;
            var str = string.Empty;
            for (int i = 3; i < data.Length; i++)
            {
                str += (char)(data[i]);
            }
            int d;
            if (Parse.TryParseInt(str, out d))
            {
                result = d;
                return true;
            }
            return false;
        }

        private static void ProcessMessage(Message m)
        {
            if (m.Data.Length == 3 && m.Data[0] == 0x18)
            {
                Speed = (short) (m.Data[1]*2);
                RPM = (short) (m.Data[2]*100);
                OnCarDataChanged();
                m.ReceiverDescription = "Speed " + Speed + " km/h " + RPM + " RPM";
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
            else if (m.Data[0] == 0x17 && m.Data.Length == 8)
            {
                Odometer = (uint) (m.Data[3]*65536 + m.Data[2]*256 + m.Data[1]);
                OnCarDataChanged();
                m.ReceiverDescription = "Odometer " + Odometer + " km";
            }
            else if (m.Data[0] == 0x19 && m.Data.Length == 4)
            {
                OutsideTemperature = m.Data[1];
                CoolantTemperature = m.Data[2];
                OnCarDataChanged();
                m.ReceiverDescription = "Outside temperature  " + OutsideTemperature + "°C, coolant temperature " + CoolantTemperature + "°C";
            }
            else if (m.Data[0] == 0x24 && m.Data.Length > 2)
            {
                switch (m.Data[1])
                {
                    case 0x01:
                        if (m.Data.Length == 10)
                        {
                            var hour = Convert.ToByte(((char)m.Data[3]).ToString()) * 10 + Convert.ToByte(((char)m.Data[4]).ToString());
                            var minutes = Convert.ToByte(((char)m.Data[6]).ToString()) * 10 + Convert.ToByte(((char)m.Data[7]).ToString());
                            var now = DateTime.Now;
                            var date = new DateTime(now.Year, now.Month, now.Day, hour, minutes, now.Second);
                            if (!_timeIsSet)
                            {
                                Utility.SetLocalTime(date);
                                _timeIsSet = true;
                            }
                            m.ReceiverDescription = "Date & Time, " + date.ToString("yyyy-MM-dd HH:mm");
                        }
                        break;
                    case 0x02:
                        if (m.Data.Length == 13)
                        {
                            var day = Convert.ToByte(((char)m.Data[3]).ToString()) * 10 + Convert.ToByte(((char)m.Data[4]).ToString());
                            var month = Convert.ToByte(((char)m.Data[6]).ToString()) * 10 + Convert.ToByte(((char)m.Data[7]).ToString());
                            var year = Convert.ToByte(((char)m.Data[9]).ToString()) * 1000 + Convert.ToByte(((char)m.Data[10]).ToString()) * 100
                                + Convert.ToByte(((char)m.Data[11]).ToString()) * 10 + Convert.ToByte(((char)m.Data[12]).ToString());
                            var now = DateTime.Now;
                            var date = new DateTime(year, month, day, now.Hour, now.Minute, now.Second);
                            if (!_dateIsSet)
                            {
                                Utility.SetLocalTime(date);
                                _dateIsSet = true;
                            }
                            m.ReceiverDescription = "Date & Time, " + date.ToString("yyyy-MM-dd HH:mm");
                        }
                        break;
                    case 0x03:
                        if (m.Data.Length == 8)
                        {
                            float temperature;
                            if (ParseFloat(m.Data, out temperature))
                            {
                                OutsideTemperature = temperature;
                                OnCarDataChanged();
                                m.ReceiverDescription = "Outside temperature  " + OutsideTemperature + "°C";
                            }
                        }
                        break;
                    case 0x04:
                        if (m.Data.Length == 7)
                        {
                            float consumption;
                            if (ParseFloat(m.Data, out consumption))
                            {
                                Consumption1 = consumption;
                                OnCarDataChanged();
                                m.ReceiverDescription = "Consumption 1  " + Consumption1 + " l/km";
                            }
                        }
                        break;
                    case 0x05:
                        if (m.Data.Length == 7)
                        {
                            float consumption;
                            if (ParseFloat(m.Data, out consumption))
                            {
                                Consumption2 = consumption;
                                OnCarDataChanged();
                                m.ReceiverDescription = "Consumption 2  " + Consumption1 + " l/km";
                            }
                        }
                        break;
                    case 0x06:
                        if (m.Data.Length == 7)
                        {
                            int range;
                            if (ParseInt(m.Data, out range))
                            {
                                Range = (short)range;
                                OnCarDataChanged();
                                m.ReceiverDescription = "Range  " + Range + " km";
                            }
                        }
                        break;
                    case 0x0A:
                        if (m.Data.Length == 7)
                        {
                            float speed;
                            if (ParseFloat(m.Data, out speed))
                            {
                                AverageSpeed = speed;
                                OnCarDataChanged();
                                m.ReceiverDescription = "Average speed  " + Consumption1 + " km/h";
                            }
                        }
                        break;
                }
            }
            else if (m.Data[0] == 0x54 && m.Data.Length == 14)
            {
                string vin = ("" + (char)m.Data[1] + (char)m.Data[2] + m.Data[3].ToHex() + m.Data[4].ToHex() + m.Data[5].ToHex()).Substring(0, 7);
                VIN = vin;
                OnCarDataChanged();
                m.ReceiverDescription = "VIN " + VIN;
            }
        }

        private static void OnCarDataChanged()
        {
            var e = CarDataChanged;
            if (e != null)
            {
                e(new CarDataEventArgs(RPM, Speed, Odometer, OutsideTemperature, CoolantTemperature));
            }
        }

        #endregion
    }

    public enum IgnitionState
    {
        Off,
        Acc,
        Ign
    }

    public static class IgnitionStateExtensions
    {
        public static string ToStringValue(this IgnitionState e)
        {
            switch (e)
            {
                case IgnitionState.Off: return "Off";
                case IgnitionState.Acc: return "Acc";
                case IgnitionState.Ign: return "Ign";
            }
            return "NotSpecified(" + e.ToString() + ")";
        }
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

    public class CarDataEventArgs : EventArgs
    {
        public short RPM { get; private set; }
        public short Speed { get; private set; }
        public uint Odometer { get; private set; }
        public float OutsideTemperature { get; private set; }
        public float CoolantTemperature { get; private set; }

        public CarDataEventArgs(short rpm, short speed, uint odometer, float outsideTemperature, float coolantTemperature)
        {
            RPM = rpm;
            Speed = speed;
            Odometer = odometer;
            OutsideTemperature = outsideTemperature;
            CoolantTemperature = coolantTemperature;
        }
    }

    public delegate void IgnitionEventHandler(IgnitionEventArgs e);

    public delegate void CarDataEventHandler(CarDataEventArgs e);
}
