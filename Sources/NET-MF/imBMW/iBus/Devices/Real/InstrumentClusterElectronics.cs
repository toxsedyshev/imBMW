using System;
using imBMW.Tools;
using System.Threading;

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
        public ushort Speed { get; private set; }
        public ushort RPM { get; private set; }

        public SpeedRPMEventArgs(ushort speed, ushort rpm)
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

    public class VinEventArgs
    {
        public string Value { get; private set; }

        public VinEventArgs(string value)
        {
            Value = value;
        }
    }

    public class OdometerEventArgs
    {
        public uint Value { get; private set; }

        public OdometerEventArgs(uint value)
        {
            Value = value;
        }
    }

    public class ConsumptionEventArgs
    {
        public float Value { get; private set; }

        public ConsumptionEventArgs(float value)
        {
            Value = value;
        }
    }

    public class AverageSpeedEventArgs
    {
        public ushort Value { get; private set; }

        public AverageSpeedEventArgs(ushort value)
        {
            Value = value;
        }
    }

    public class RangeEventArgs
    {
        public uint Value { get; private set; }

        public RangeEventArgs(uint value)
        {
            Value = value;
        }
    }

    public class DateTimeEventArgs
    {
        public DateTime Value { get; private set; }

        public bool DateIsSet { get; private set; }

        public DateTimeEventArgs(DateTime value, bool dateIsSet = false)
        {
            Value = value;
            DateIsSet = dateIsSet;
        }
    }

    public delegate void IgnitionEventHandler(IgnitionEventArgs e);

    public delegate void SpeedRPMEventHandler(SpeedRPMEventArgs e);

    public delegate void TemperatureEventHandler(TemperatureEventArgs e);

    public delegate void VinEventHandler(VinEventArgs e);

    public delegate void OdometerEventHandler(OdometerEventArgs e);

    public delegate void ConsumptionEventHandler(ConsumptionEventArgs e);

    public delegate void AverageSpeedEventHandler(AverageSpeedEventArgs e);

    public delegate void RangeEventHandler(RangeEventArgs e);

    public delegate void DateTimeEventHandler(DateTimeEventArgs e);

    #endregion


    public static class InstrumentClusterElectronics
    {
        static IgnitionState currentIgnitionState = IgnitionState.Off;
        
        public static ushort CurrentRPM { get; private set; }
        public static ushort CurrentSpeed { get; private set; }

        public static string VIN { get; private set; }
        public static uint Odometer { get; private set; }

        public static float Consumption1 { get; private set; }
        public static float Consumption2 { get; private set; }

        public static uint Range { get; private set; }
        public static ushort AverageSpeed { get; private set; }

        public static sbyte TemperatureOutside { get; private set; }
        public static sbyte TemperatureCoolant { get; private set; }
        
        static readonly Message MessageRequestTime = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Request Time", 0x41, 0x01, 0x01);
        static readonly Message MessageRequestDate = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Request Date", 0x41, 0x02, 0x01);

        static readonly Message MessageGong1 = new Message(DeviceAddress.Radio, DeviceAddress.InstrumentClusterElectronics, "Gong 1", 0x23, 0x62, 0x30, 0x37, 0x08);
        static readonly Message MessageGong2 = new Message(DeviceAddress.Radio, DeviceAddress.InstrumentClusterElectronics, "Gong 2", 0x23, 0x62, 0x30, 0x37, 0x10);

        private const int _getDateTimeTimeout = 1000;

        private static ManualResetEvent _getDateTimeSync = new ManualResetEvent(false);
        private static DateTimeEventArgs _getDateTimeResult;

        private static bool _timeIsSet, _dateIsSet;
        private static byte _timeHour, _timeMinute, _dateDay, _dateMonth;
        private static ushort _dateYear;

        static InstrumentClusterElectronics()
        {
            TemperatureOutside = sbyte.MinValue;
            TemperatureCoolant = sbyte.MinValue;

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, ProcessIKEMessage);
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessIKEMessage(Message m)
        {
            if (m.Data.Length == 3 && m.Data[0] == 0x18)
            {
                OnSpeedRPMChanged((ushort)(m.Data[1] * 2), (ushort)(m.Data[2] * 100));
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
            else if (m.Data[0] == 0x17 && m.Data.Length == 8)
            {
                OnOdometerChanged((uint)(m.Data[3] * 65536 + m.Data[2] * 256 + m.Data[1]));
                m.ReceiverDescription = "Odometer " + Odometer + " km";
            }
            else if (m.Data[0] == 0x54 && m.Data.Length == 14)
            {
                OnVinChanged("" + (char)m.Data[1] + (char)m.Data[2] + m.Data[3].ToHex() + m.Data[4].ToHex() + m.Data[5].ToHex().Substring(0, 7));
                m.ReceiverDescription = "VIN " + VIN;
            }
            else if (m.Data.Length == 4 && m.Data[0] == 0x19)
            {
                OnTemperatureChanged((sbyte)m.Data[1], (sbyte)m.Data[2]);
                m.ReceiverDescription = "Temperature. Outside " + TemperatureOutside + "°C, Coolant " + TemperatureCoolant + "°C";
            }
            else if (m.Data[0] == 0x24 && m.Data.Length > 2)
            {
                switch (m.Data[1])
                {
                    case 0x01:
                        if (m.Data.Length == 10)
                        {
                            var hourStr = new string(new[] { (char)m.Data[3], (char)m.Data[4] });
                            var minuteStr = new string(new[] { (char)m.Data[6], (char)m.Data[7] });
                            if (hourStr == "--" || minuteStr == "--")
                            {
                                m.ReceiverDescription = "Time: unset";
                                break;
                            }
                            var hour = Convert.ToByte(hourStr);
                            var minute = Convert.ToByte(minuteStr);
                            if (hour == 12 && m.Data[8] == 0x41) // 12AM
                            {
                                hour = 0;
                            }
                            if (m.Data[8] == 0x50) // PM
                            {
                                hour += 12;
                            }
                            OnTimeChanged(hour, minute);
                            m.ReceiverDescription = "Time: " + hour + ":" + minute;
                        }
                        break;
                    case 0x02:
                        if (m.Data.Length == 13)
                        {
                            var dayStr = new string(new[] { (char)m.Data[3], (char)m.Data[4] });
                            var monthStr = new string(new[] { (char)m.Data[6], (char)m.Data[7] });
                            var yearStr = new string(new[] { (char)m.Data[9], (char)m.Data[10], (char)m.Data[11], (char)m.Data[12] });
                            if (dayStr == "--" || monthStr == "--" || yearStr == "----") // year is always set
                            {
                                m.ReceiverDescription = "Date: unset";
                                break;
                            }
                            var day = Convert.ToByte(dayStr);
                            var month = Convert.ToByte(monthStr);
                            var year = Convert.ToUInt16(yearStr);
                            if (m.Data[5] == 0x2F || month > 12 && day <= 12)
                            {
                                // TODO use region settings
                                var t = day;
                                day = month;
                                month = t;
                            }
                            OnDateChanged(day, month, year);
                            m.ReceiverDescription = "Date: " + day + "/" + month + "/" + year;
                        }
                        break;
                    case 0x03:
                        if (m.Data.Length == 8)
                        {
                            float temperature;
                            if (m.Data.ParseFloat(out temperature))
                            {
                                //TemperatureOutside = (sbyte)temperature;
                                m.ReceiverDescription = "Outside temperature  " + temperature + "°C";
                            }
                        }
                        break;
                    case 0x04:
                        if (m.Data.Length == 7)
                        {
                            float consumption;
                            if (m.Data.ParseFloat(out consumption))
                            {
                                OnConsumptionChanged(true, consumption);
                                m.ReceiverDescription = "Consumption 1  " + Consumption1 + " l/km";
                            }
                        }
                        break;
                    case 0x05:
                        if (m.Data.Length == 7)
                        {
                            float consumption;
                            if (m.Data.ParseFloat(out consumption))
                            {
                                OnConsumptionChanged(false, consumption);
                                m.ReceiverDescription = "Consumption 2  " + Consumption2 + " l/km";
                            }
                        }
                        break;
                    case 0x06:
                        if (m.Data.Length == 7)
                        {
                            int range;
                            if (m.Data.ParseInt(out range))
                            {
                                OnRangeChanged((uint)range);
                                m.ReceiverDescription = "Range  " + Range + " km";
                            }
                        }
                        break;
                    case 0x0A:
                        if (m.Data.Length == 7)
                        {
                            float speed;
                            if (m.Data.ParseFloat(out speed))
                            {
                                OnAverageSpeedChanged((ushort)speed);
                                m.ReceiverDescription = "Average speed  " + AverageSpeed + " km/h";
                            }
                        }
                        break;
                }
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

        public static void RequestDateTime()
        {
            Manager.EnqueueMessage(MessageRequestDate, MessageRequestTime);
        }

        public static DateTimeEventArgs GetDateTime()
        {
            return GetDateTime(_getDateTimeTimeout);
        }

        public static DateTimeEventArgs GetDateTime(int timeout)
        {
            _getDateTimeSync.Reset();
            _getDateTimeResult = null;
            DateTimeChanged += GetDateTimeCallback;
            RequestDateTime();
            _getDateTimeSync.WaitOne(timeout, true);
            DateTimeChanged -= GetDateTimeCallback;
            return _getDateTimeResult;
        }

        private static void GetDateTimeCallback(DateTimeEventArgs e)
        {
            _getDateTimeResult = e;
            _getDateTimeSync.Set();
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

        private static void OnSpeedRPMChanged(ushort speed, ushort rpm)
        {
            CurrentSpeed = speed;
            CurrentRPM = rpm;
            var e = SpeedRPMChanged;
            if (e != null)
            {
                e(new SpeedRPMEventArgs(CurrentSpeed, CurrentRPM));
            }
        }

        private static void OnVinChanged(string vin)
        {
            VIN = vin;
            var e = VinChanged;
            if (e != null)
            {
                e(new VinEventArgs(vin));
            }
        }

        private static void OnOdometerChanged(uint odometer)
        {
            Odometer = odometer;
            var e = OdometerChanged;
            if (e != null)
            {
                e(new OdometerEventArgs(odometer));
            }
        }

        private static void OnAverageSpeedChanged(ushort averageSpeed)
        {
            AverageSpeed = averageSpeed;
            var e = AverageSpeedChanged;
            if (e != null)
            {
                e(new AverageSpeedEventArgs(averageSpeed));
            }
        }

        private static void OnRangeChanged(uint range)
        {
            Range = range;
            var e = RangeChanged;
            if (e != null)
            {
                e(new RangeEventArgs(range));
            }
        }

        private static void OnConsumptionChanged(bool isFirst, float value)
        {
            ConsumptionEventHandler e;
            if (isFirst)
            {
                e = Consumption1Changed;
                Consumption1 = value;
            }
            else
            {
                e = Consumption2Changed;
                Consumption2 = value;
            }
            if (e != null)
            {
                e(new ConsumptionEventArgs(value));
            }
        }

        private static void OnTimeChanged(byte hour, byte minute)
        {
            if (_timeIsSet && _timeHour == hour && _timeMinute == minute)
            {
                return;
            }
            _timeHour = hour;
            _timeMinute = minute;
            _timeIsSet = true;
            OnDateTimeChanged();
        }

        private static void OnDateChanged(byte day, byte month, ushort year)
        {
            if (_dateIsSet && _dateDay == day && _dateMonth == month && _dateYear == year)
            {
                return;
            }
            _dateDay = day;
            _dateMonth = month;
            _dateYear = year;
            _dateIsSet = true;
        }

        private static void OnDateTimeChanged()
        {
            if (!_timeIsSet)
            {
                return;
            }
            var now = DateTime.Now;
            if (!_dateIsSet)
            {
                _dateYear = (ushort)now.Year;
                _dateMonth = (byte)now.Month;
                _dateDay = (byte)now.Day;
            }
            now = new DateTime(_dateYear, _dateMonth, _dateDay, _timeHour, _timeMinute, now.Second);
            OnDateTimeChanged(now, _dateIsSet);
        }

        private static void OnDateTimeChanged(DateTime value, bool dateIsSet = false)
        {
            var e = DateTimeChanged;
            if (e != null)
            {
                e(new DateTimeEventArgs(value, dateIsSet));
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
        
        /// <summary>
        /// IKE sends VIN every TBD sec
        /// </summary>
        public static event VinEventHandler VinChanged;

        /// <summary>
        /// IKE sends odometer value every TBD sec
        /// </summary>
        public static event OdometerEventHandler OdometerChanged;

        /// <summary>
        /// IKE sends consumption1 information every TBD sec
        /// </summary>
        public static event ConsumptionEventHandler Consumption1Changed;

        /// <summary>
        /// IKE sends consumption2 information every TBD sec
        /// </summary>
        public static event ConsumptionEventHandler Consumption2Changed;

        /// <summary>
        /// IKE sends average speed information every TBD sec
        /// </summary>
        public static event AverageSpeedEventHandler AverageSpeedChanged;

        /// <summary>
        /// IKE sends range information every TBD sec
        /// </summary>
        public static event RangeEventHandler RangeChanged;

        /// <summary>
        /// IKE sends time and date (optional) on request
        /// </summary>
        public static event DateTimeEventHandler DateTimeChanged;
    }
}
