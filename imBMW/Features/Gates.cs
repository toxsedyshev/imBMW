using System;
using System.Collections;
using System.Threading;
using imBMW.Multimedia;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    public static class Gates
    {
        #region Private types

        private class GatePoint
        {
            public float Latitude { get; private set; }
            public float Longitude { get; private set; }

            public float MaxDistance { get; private set; }

            public GateToggleMethod ToggleMethod { get; private set; }
            public object[] ToggleMethodArguments { get; private set; }

            public GatePoint(float latitude, float longitude, float distance, GateToggleMethod method, object[] methodArguments)
            {
                Latitude = latitude;
                Longitude = longitude;
                MaxDistance = distance;
                ToggleMethod = method;
                ToggleMethodArguments = methodArguments;
            }
        }

        #endregion

        #region Private static fields

        private static readonly QueueThreadWorker ThreadWorker = new QueueThreadWorker(ProcessItem, ThreadPriority.Highest);

        private static bool _initialized;
        private static readonly object LockObject = new object();
        private static readonly ArrayList Objects = new ArrayList();

        private static RCSwitch _switch;

        #endregion
        
        #region Static constructor

        static Gates()
        {
            Navigation.CoordinatesChangedReceived += args => ThreadWorker.EnqueueArray(args.NewCoordinates);
        }

        #endregion

        #region Private static methods

        private static void ProcessItem(object item)
        {
            var coordinates = item as Coordinate;
            if (coordinates == null) return;
            lock (LockObject)
            {
                if (!_initialized) return;

                foreach (GatePoint gatePoint in Objects)
                {
                    var distance = coordinates.Distance(gatePoint.Latitude, gatePoint.Longitude);
                    if (distance < gatePoint.MaxDistance)
                    {
                        switch (gatePoint.ToggleMethod)
                        {
                            case GateToggleMethod.Send433MhzSignal:
                                foreach (var arg in gatePoint.ToggleMethodArguments)
                                {
                                    var code = arg.ToString().ToUlong();
                                    _switch.Send(code, 24);
                                }
                                break;
                        }
                    }
                }
            }
        }

        #endregion

        #region Public static methods

        public static void Init(RCSwitch rcSwitch)
        {
            lock (LockObject)
            {
                if (_initialized) return;
                _switch = rcSwitch;
                _initialized = true;
            }
        }

        public static void AddGatesObserver(float latitude, float longitude, float distance, GateToggleMethod method, object[] methodArguments)
        {
            lock (LockObject)
            {
                if (!_initialized) throw new Exception("Gates are not initialized yet");
                if (methodArguments == null || methodArguments.Length == 0) throw new ApplicationException("Invalid or empty arguments specified");
                Objects.Add(new GatePoint(latitude, longitude, distance, method, methodArguments));
            }
        }

        #endregion
    }

    public enum GateToggleMethod
    {
        Send433MhzSignal,
    }
}
