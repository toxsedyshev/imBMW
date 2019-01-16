using System;
using imBMW.Tools;
using System.Threading;
using Microsoft.SPOT.Hardware;

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
        public float Value { get; private set; }

        public AverageSpeedEventArgs(float value)
        {
            Value = value;
        }
    }

    public class SpeedLimitEventArgs
    {
        public ushort Value { get; private set; }

        public SpeedLimitEventArgs(ushort value)
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
    
    public delegate void ConsumptionEventHandler(ConsumptionEventArgs e);

    public delegate void AverageSpeedEventHandler(AverageSpeedEventArgs e);

    public delegate void SpeedLimitEventHandler(SpeedLimitEventArgs e);

    public delegate void RangeEventHandler(RangeEventArgs e);

    public delegate void DateTimeEventHandler(DateTimeEventArgs e);

    #endregion


    public static class InstrumentClusterElectronics
    {
        static IgnitionState currentIgnitionState = IgnitionState.Off;

        public const int DisplayTextMaxLength = 20;

        const int displayTextDelay = 200;

        public static ushort CurrentRPM { get; private set; }
        public static ushort CurrentSpeed { get; private set; }

        public static string VIN { get; private set; }
        public static uint Odometer { get; private set; }

        public static float Consumption1 { get; private set; }
        public static float Consumption2 { get; private set; }

        public static uint Range { get; private set; }
        public static float AverageSpeed { get; private set; }
        public static ushort SpeedLimit { get; private set; }

        public static sbyte TemperatureOutside { get; private set; }
        public static sbyte TemperatureCoolant { get; private set; }
        
        static readonly Message MessageRequestTime = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Request Time", 0x41, 0x01, 0x01);
        static readonly Message MessageRequestDate = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Request Date", 0x41, 0x02, 0x01);

        static readonly Message MessageGong1 = new Message(DeviceAddress.Radio, DeviceAddress.InstrumentClusterElectronics, "Gong 1", 0x23, 0x62, 0x30, 0x37, 0x08);
        static readonly Message MessageGong2 = new Message(DeviceAddress.Radio, DeviceAddress.InstrumentClusterElectronics, "Gong 2", 0x23, 0x62, 0x30, 0x37, 0x10);

        static readonly Message MessageResetConsumption1 = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Reset Consumption 1", 0x41, 0x04, 0x10);
        static readonly Message MessageResetConsumption2 = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Reset Consumption 2", 0x41, 0x05, 0x10);
        static readonly Message MessageResetAverageSpeed = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Reset Avgerage Speed", 0x41, 0x0A, 0x10);

        static readonly Message MessageSpeedLimitCurrentSpeed = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Speed Limit to Current Speed", 0x41, 0x09, 0x20);
        static readonly Message MessageSpeedLimitOff = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Speed Limit OFF", 0x41, 0x09, 0x08);
        static readonly Message MessageSpeedLimitOn = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Speed Limit ON", 0x41, 0x09, 0x04);

        private const int _getDateTimeTimeout = 1000;

        private static Thread _getDateTimeThread;
        private static ManualResetEvent _getDateTimeSync;
        private static DateTimeEventArgs _getDateTimeResult;

        static Timer displayTextDelayTimer;

        private static bool _timeIsSet, _dateIsSet;
        private static byte _timeHour, _timeMinute, _dateDay, _dateMonth;

        private static ushort _dateYear;
        private static ushort _lastSpeedLimit;

        private static Timer engineStartedTimer;

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
                OnOdometerChanged((uint)(m.Data[3] << 16 + m.Data[2] << 8 + m.Data[1]));
                m.ReceiverDescription = "Odometer " + Odometer + " km";
            }
            else if (m.Data[0] == 0x54 && m.Data.Length == 14)
            {
                OnVinChanged("" + (char)m.Data[1] + (char)m.Data[2] + m.Data[3].ToHex() + m.Data[4].ToHex() + m.Data[5].ToHex()[0]);
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
                            if (hour == 12 && m.Data[8] == 'A') // 12AM
                            {
                                hour = 0;
                            }
                            else if (hour != 12 && m.Data[8] == 'P') // PM < 12
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
                        if (m.Data.Length >= 8)
                        {
                            float temperature;
                            if (m.Data.ParseFloat(out temperature, 3, 5))
                            {
                                //TemperatureOutside = (sbyte)temperature;
                                m.ReceiverDescription = "Outside temperature  " + temperature + "°C";
                            }
                        }
                        break;
                    case 0x04:
                    case 0x05:
                        if (m.Data.Length >= 7)
                        {
                            float consumption = 0;
                            m.Data.ParseFloat(out consumption, 3, 4);
                            OnConsumptionChanged(m.Data[1] == 0x04, consumption);
                            m.ReceiverDescription = "Consumption " + (m.Data[1] == 0x04 ? 1 : 2) + " = " + consumption + " l/km";
                        }
                        break;
                    case 0x06:
                        if (m.Data.Length >= 7)
                        {
                            int range;
                            if (m.Data.ParseInt(out range, 3, 4))
                            {
                                OnRangeChanged((uint)range);
                                m.ReceiverDescription = "Range  " + Range + " km";
                            }
                        }
                        break;
                    case 0x09:
                        if (m.Data.Length >= 7)
                        {
                            int speedLimit;
                            if (m.Data.ParseInt(out speedLimit, 3, 3))
                            {
                                OnSpeedLimitChanged((ushort)speedLimit);
                                m.ReceiverDescription = "Speed limit  " + SpeedLimit + " km/h";
                            }
                        }
                        break;
                    case 0x0A:
                        if (m.Data.Length >= 7)
                        {
                            float speed = 0;
                            m.Data.ParseFloat(out speed, 3, 4);
                            OnAverageSpeedChanged(speed);
                            m.ReceiverDescription = "Average speed  " + AverageSpeed + " km/h";
                        }
                        break;
                }
            }
            else if (m.DestinationDevice == DeviceAddress.FrontDisplay && m.Data.Compare(0x2A, 0x00, 0x00))
            {
                OnSpeedLimitChanged(0);
                m.ReceiverDescription = "Speed limit turned off";
            }
            else if (m.DestinationDevice == DeviceAddress.FrontDisplay && m.Data.Compare(0x2A, 0x02, 0x00))
            {
                OnSpeedLimitChanged(_lastSpeedLimit);
                m.ReceiverDescription = "Speed limit turned on";
            }
            // TODO arrive time, arrive distance, timers
        }

        public static void DisplayText(string s, TextAlign align)
        {
            ClearDisplayTextTimer();

            byte[] data = new byte[] { 0x23, 0x42, 0x32 };
            data = data.PadRight(0x20, DisplayTextMaxLength);
            data.PasteASCII(s.Translit(), 3, DisplayTextMaxLength, align);
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.InstrumentClusterElectronics, "Show text \"" + s + "\" on IKE", data));
        }

        public static void DisplayTextWithDelay(string s, TextAlign align)
        {
            DisplayTextWithDelay(s, align, displayTextDelay);
        }

        public static void DisplayTextWithDelay(string s, TextAlign align, int delay)
        {
            ClearDisplayTextTimer();

            displayTextDelayTimer = new Timer(delegate
            {
                DisplayText(s, align);
            }, null, delay, 0);
        }

        static void ClearDisplayTextTimer()
        {
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
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

        public static void ResetConsumption1()
        {
            Manager.EnqueueMessage(MessageResetConsumption1);
        }

        public static void ResetConsumption2()
        {
            Manager.EnqueueMessage(MessageResetConsumption2);
        }

        public static void ResetAverageSpeed()
        {
            Manager.EnqueueMessage(MessageResetAverageSpeed);
        }

        public static void SetSpeedLimitToCurrentSpeed()
        {
            Manager.EnqueueMessage(MessageSpeedLimitCurrentSpeed);
        }

        public static void SetSpeedLimitOff()
        {
            Manager.EnqueueMessage(MessageSpeedLimitOff);
        }

        public static void SetSpeedLimitOn()
        {
            SetSpeedLimit(_lastSpeedLimit == 0 ? (ushort)1 : _lastSpeedLimit);
        }

        public static void SetSpeedLimit(ushort limit)
        {
            if (limit <= 0)
            {
                SetSpeedLimitOff();
                return;
            }
            if (limit < 10) // TODO check mph
            {
                limit = 10;
            }
            if (limit > 300) // TODO fix region settings
            {
                limit = 300;
            }
            var refresh = SpeedLimit == 0;
            if (limit != _lastSpeedLimit)
            {
                if (refresh)
                {
                    _lastSpeedLimit = limit;
                }
                Manager.EnqueueMessage(new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, "Set speed limit", 0x40, 0x09, (byte)(limit >> 8), (byte)(limit & 0xFF)));
            }
            if (refresh)
            {
                Manager.EnqueueMessage(MessageSpeedLimitOn);
            }
        }

        public static void IncreaseSpeedLimit(ushort add = 5)
        {
            SetSpeedLimit((ushort)(SpeedLimit + add));
        }

        public static void DecreaseSpeedLimit(ushort sub = 5)
        {
            if (sub >= SpeedLimit)
            {
                SetSpeedLimitOff();
            }
            else
            {
                SetSpeedLimit((ushort)(SpeedLimit - sub));
            }
        }

        public static void RequestDateTime()
        {
            Manager.EnqueueMessage(MessageRequestDate, MessageRequestTime);
        }

        public static void RequestDateTimeAndSetLocal(bool separateThread = true)
        {
            if (separateThread)
            {
                _getDateTimeThread = new Thread(() =>
                {
                    RequestDateTimeAndSetLocal(false);
                    _getDateTimeThread = null;
                });
                _getDateTimeThread.Start();
            }
            else
            {
                var dateTime = GetDateTime();
                if (dateTime != null)
                {
                    Logger.Info("Set Date and Time: " + dateTime.Value.ToString("yy-MM-dd HH:mm:ss"));
                    Utility.SetLocalTime(dateTime.Value);
                    Logger.Info("Done Set Date and Time");
                }
                else
                {
                    Logger.Warning("Date and Time wasn't received");
                }
            }
        }

        public static DateTimeEventArgs GetDateTime()
        {
            return GetDateTime(_getDateTimeTimeout);
        }

        public static DateTimeEventArgs GetDateTime(int timeout)
        {
            lock (_getDateTimeResult)
            {
                _getDateTimeSync = new ManualResetEvent(false);
                _getDateTimeSync.Reset();
                _getDateTimeResult = null;
                DateTimeChanged += GetDateTimeCallback;
                RequestDateTime();
#if NETMF
                _getDateTimeSync.WaitOne(timeout, true);
#else
                _getDateTimeSync.WaitOne(timeout);
#endif
                DateTimeChanged -= GetDateTimeCallback;
                _getDateTimeSync = null;
                return _getDateTimeResult;
            }
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
            if (TemperatureOutside == outside && TemperatureCoolant == coolant)
            {
                return;
            }
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
            if (CurrentSpeed == speed && CurrentRPM == rpm)
            {
                return;
            }
            var oldRPM = CurrentRPM;
            CurrentSpeed = speed;
            CurrentRPM = rpm;
            var e = SpeedRPMChanged;
            if (e != null)
            {
                e(new SpeedRPMEventArgs(CurrentSpeed, CurrentRPM));
            }
            if (oldRPM == 0 && rpm > 0)
            {
                RunEngineStartedTimer();
            }
        }

        private static void RunEngineStartedTimer()
        {
            if (engineStartedTimer != null)
            {
                engineStartedTimer.Dispose();
                engineStartedTimer = null;
            }

            engineStartedTimer = new Timer(delegate
            {
                if (CurrentIgnitionState == IgnitionState.Ign 
                    && CurrentRPM > 0
                    && EngineStarted != null)
                {
                    EngineStarted.Invoke();
                }
            }, null, 2000, 0);
        }

        private static void OnVinChanged(string vin)
        {
            if (VIN == vin)
            {
                return;
            }
            VIN = vin;
            var e = VinChanged;
            if (e != null)
            {
                e(new VinEventArgs(vin));
            }
        }

        private static void OnOdometerChanged(uint odometer)
        {
            if (Odometer == odometer)
            {
                return;
            }
            Odometer = odometer;
            var e = OdometerChanged;
            if (e != null)
            {
                e(new RangeEventArgs(odometer));
            }
        }

        private static void OnAverageSpeedChanged(float averageSpeed)
        {
            if (AverageSpeed == averageSpeed)
            {
                return;
            }
            AverageSpeed = averageSpeed;
            var e = AverageSpeedChanged;
            if (e != null)
            {
                e(new AverageSpeedEventArgs(averageSpeed));
            }
        }

        private static void OnRangeChanged(uint range)
        {
            if (Range == range)
            {
                return;
            }
            Range = range;
            var e = RangeChanged;
            if (e != null)
            {
                e(new RangeEventArgs(range));
            }
        }

        private static void OnSpeedLimitChanged(ushort speedLimit)
        {
            if (SpeedLimit == speedLimit)
            {
                return;
            }
            SpeedLimit = speedLimit;
            if (speedLimit != 0)
            {
                _lastSpeedLimit = speedLimit;
            }
            var e = SpeedLimitChanged;
            if (e != null)
            {
                e(new SpeedLimitEventArgs(speedLimit));
            }
        }

        private static void OnConsumptionChanged(bool isFirst, float value)
        {
            ConsumptionEventHandler e;
            if (isFirst)
            {
                if (Consumption1 == value)
                {
                    return;
                }
                e = Consumption1Changed;
                Consumption1 = value;
            }
            else
            {
                if (Consumption2 == value)
                {
                    return;
                }
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
        /// Fired after 2-4 (depending on IKE message timing) after RPM become greater than 0
        /// </summary>
        public static event Action EngineStarted;

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
        public static event RangeEventHandler OdometerChanged;

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

        public static event SpeedLimitEventHandler SpeedLimitChanged;
    }
}
