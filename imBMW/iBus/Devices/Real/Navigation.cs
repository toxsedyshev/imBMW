using System;
using imBMW.Tools;
using Microsoft.SPOT;
using Math = imBMW.Tools.Math;

namespace imBMW.iBus.Devices.Real
{
    public static class Navigation
    {
        #region Public static fields

        public static event CoordinatesChangedEventHandler CoordinatesChangedReceived;

        #endregion

        #region Private static fields

        private static bool _timeIsSet;

        #endregion

        #region Static constructor

        static Navigation()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.NavigationEurope, ProcessMessage);
        }

        #endregion

        #region Public static properties

        public static Coordinate CurrentCoordinate { get; private set; }
        public static string CurrentCity { get; private set; }
        public static string CurrentStreet { get; private set; }

        #endregion

        #region Private static methods

        private static float CalculateCoordinates(byte degrees, byte minutes, byte seconds, byte secondsParts)
        {
            var deg = Convert.ToByte(degrees.ToHex());
            var min = Convert.ToByte(minutes.ToHex()) / 60f;
            var sec = Convert.ToByte(seconds.ToHex()) + Convert.ToByte(secondsParts.ToHex()) / 100f;
            sec = sec / 3600f;
            return deg + min + sec;
        }

        private static float CalculateAltitude(byte alt1, byte alt2)
        {
            return Convert.ToByte(alt1.ToHex()) * 100 + Convert.ToByte(alt2.ToHex());
        }

        private static void OnCoordinatesReceived(Message m)
        {
            var speed = InstrumentClusterElectronics.CurrentSpeed;
            var dateTime = DateTime.UtcNow;
            var latitude = CalculateCoordinates(m.Data[3], m.Data[4], m.Data[5], m.Data[6]);
            var longitude = CalculateCoordinates(m.Data[8], m.Data[9], m.Data[10], m.Data[11]);
            var altitude = 0f;
            bool isFixed = m.Data[1] == 0x01;
            if (isFixed)
            {
                altitude = CalculateAltitude(m.Data[12], m.Data[13]);
                dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                                        Convert.ToByte(m.Data[15].ToHex()), Convert.ToByte(m.Data[16].ToHex()), Convert.ToByte(m.Data[17].ToHex()));
                isFixed = _timeIsSet;
            }
            var coordinates = new Coordinate(latitude, longitude, altitude, speed, dateTime, isFixed);
            m.ReceiverDescription = coordinates.ToString();

            CurrentCoordinate = coordinates;

            var args = new CoordinatesChangedEventArgs { NewCoordinates = coordinates };
            var e = CoordinatesChangedReceived;
            if (e != null)
            {
                e(args);
            }
        }

        private static void ProcessMessage(Message m)
        {
            if (m.Data[0] == 0xA2 && m.Data.Length == 18)
            {
                OnCoordinatesReceived(m);
            }
            else if (m.Data[0] == 0xA4 && m.Data.Length == 33)
            {
                var builder = new StringBuilder();
                for (int i = 3; i < m.Data.Length; i++)
                {
                    if (m.Data[i] == 0x00) break;
                    builder.Append((char)m.Data[i]);
                }
                if (m.Data[2] == 0x01)
                {
                    CurrentCity = builder.ToString().TrimEnd(new[] { ';', ',' });
                    m.ReceiverDescription = "City: " + CurrentCity;
                }
                else if (m.Data[2] == 0x02)
                {
                    CurrentStreet = builder.ToString().TrimEnd(new[] { ';', ',' });
                    m.ReceiverDescription = "Street: " + CurrentStreet;
                }
                else
                {
                    m.ReceiverDescription = "Unknown: " + builder;
                }
            }
            else if (m.Data[0] == 0x1F && m.Data.Length == 9)
            {
                var hour = Convert.ToByte(m.Data[2].ToHex());
                var minutes = Convert.ToByte(m.Data[3].ToHex());
                var day = Convert.ToByte(m.Data[4].ToHex());
                var month = Convert.ToByte(m.Data[6].ToHex());
                var year = Convert.ToByte(m.Data[7].ToHex()) * 100 + Convert.ToByte(m.Data[8].ToHex());
                var date = new DateTime(year, month, day, hour, minutes, 0);
                if (!_timeIsSet)
                {
                    //Utility.SetLocalTime(date);
                    _timeIsSet = true;
                }
                m.ReceiverDescription = "Date & Time, " + date.ToString("yyyy-MM-dd HH:mm");
            }
            else if (m.Data[0] == 0x44 && m.Data.Length == 3)
            {
                if (m.Data[1] == 0x21 || m.Data[1] == 0x29)
                {
                    int mult = m.Data[1] == 0x29 ? 10 : 1;
                    m.ReceiverDescription = "IKE Text, Distance: " + Convert.ToByte(m.Data[2].ToHex()) * mult;
                }
                else if (m.Data[1] == 0x20)
                {
                    m.ReceiverDescription = "IKE Text, Clear";
                }
            }
        }

        #endregion
    }

    public class Coordinate
    {
        #region Public constructors

        public Coordinate()
        {
        }

        public Coordinate(float latitude, float longitude, float altitude,
                          ushort speed, DateTime time, bool isFixed)
        {
            Altitude = altitude;
            Latitude = latitude;
            Longitude = longitude;
            Speed = speed;
            Time = time;
            IsFixed = isFixed;
        }

        #endregion

        #region Public properties

        public float Altitude { get; private set; }

        public float Latitude { get; private set; }

        public float Longitude { get; private set; }

        public ushort Speed { get; private set; }

        public bool IsFixed { get; private set; }

        public DateTime Time { get; private set; }

        #endregion

        #region Public methods

        public double Distance(float lat, float lng)
        {
            return Distance(Latitude, Longitude, lat, lng);
        }

        public double Distance(Coordinate coordinate)
        {
            return Distance(Latitude, Longitude, coordinate.Latitude, coordinate.Longitude);
        }

        #endregion

        #region Public static methods

        private const float EarthRadiusInKilometers = 6367f;

        public static double Distance(float lat1, float lng1, float lat2, float lng2)
        {
            return Distance(lat1, lng1, lat2, lng2, EarthRadiusInKilometers);
        }

        #endregion

        #region Private static methods

        private static double Distance(float lat1, float lng1, float lat2, float lng2, float radius)
        {
            // Implements the Haversine formula http://en.wikipedia.org/wiki/Haversine_formula
            var lat = Math.ToRadians(lat2 - lat1);
            var lng = Math.ToRadians(lng2 - lng1);
            var sinLat = Math.Sin(0.5 * lat);
            var sinLng = Math.Sin(0.5 * lng);
            var cosLat1 = Math.Cos(Math.ToRadians(lat1));
            var cosLat2 = Math.Cos(Math.ToRadians(lat2));
            var h1 = sinLat * sinLat + cosLat1 * cosLat2 * sinLng * sinLng;
            var h2 = Math.Sqrt(h1);
            var h3 = 2 * Math.Asin(Math.Min(1, h2));
            return radius * h3;
        }

        #endregion

        #region Public overriden methods

        public override string ToString()
        {
            return StringHelpers.Format("{0} {1}, {2}m", Latitude, Longitude, Altitude);
        }

        #endregion
    }

    public class CoordinatesChangedEventArgs : EventArgs
    {
        public Coordinate NewCoordinates { get; set; }
    }

    public delegate void CoordinatesChangedEventHandler(CoordinatesChangedEventArgs args);
}
