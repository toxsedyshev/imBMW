using System;
using System.Collections;
using System.Threading;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace imBMW.Features
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

        private static readonly QueueThreadWorker ThreadWorker = new QueueThreadWorker(ProcessItem);

        private static bool _initialized;
        private static readonly object LockObject = new object();
        private static readonly ArrayList Objects = new ArrayList();

        private static Thread _sendThread;
        private static bool _exitSendThread;

        //private static Coordinate LastCoordinate;
        private static Coordinate _currentCoordinate;

        private static RCSwitch _switch;

        #endregion

        #region Static constructor

        static Gates()
        {
            Navigation.CoordinatesChangedReceived += args => ThreadWorker.EnqueueArray(args.NewCoordinates);
        }

        #endregion

        #region Private static methods

        private static bool NeedSend()
        {
            lock (LockObject)
            {
                foreach (GatePoint gatePoint in Objects)
                {
                    var distance = _currentCoordinate.Distance(gatePoint.Latitude, gatePoint.Longitude);
                    if (distance < gatePoint.MaxDistance)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static void SendThread()
        {
			for (int i = 0; i < 10 && !_exitSendThread; i++)
            {
                lock (LockObject)
                {
                    foreach (GatePoint gatePoint in Objects)
                    {
                        var distance = _currentCoordinate.Distance(gatePoint.Latitude, gatePoint.Longitude);
                        if (distance < gatePoint.MaxDistance)
                        {
                            Logger.Info("Sending signal to gates to open");
                            switch (gatePoint.ToggleMethod)
                            {
                                case GateToggleMethod.Send433MhzSignal:
                                    foreach (var arg in gatePoint.ToggleMethodArguments)
                                    {
										var code = Parse(arg.ToString());
                                        _switch.Send(code, 24);
                                    }
                                    break;
                            }
                        }
                    }
                }
                Thread.Sleep(10000);
            }
        }

		public static ulong Parse(string hex)
		{
			if (StringHelpers.IsNullOrEmpty(hex)) throw new ArgumentException("hex");

			int i = hex.Length > 1 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X') ? 2 : 0;
			ulong value = 0;

			while (i < hex.Length)
			{
				uint x = hex[i++];

				if (x >= '0' && x <= '9') x = x - '0';
				else if (x >= 'A' && x <= 'F') x = (x - 'A') + 10;
				else if (x >= 'a' && x <= 'f') x = (x - 'a') + 10;
				else throw new ArgumentOutOfRangeException("hex");

				value = 16 * value + x;

			}

			return value;
		}

        private static void ProcessItem(object item)
        {
            var coordinates = item as Coordinate;
            if (coordinates == null) return;
            lock (LockObject)
            {
                if (!_initialized) return;

                //LastCoordinate = _currentCoordinate;
                _currentCoordinate = coordinates;

                bool needSend = NeedSend();
                if (_sendThread != null && _sendThread.IsAlive)
                {
                    // Check the last coordinate if there is a need to stop the thread
                    if (!needSend)
                    {
                        _exitSendThread = true;
                        _sendThread.Join();
                        _sendThread = null;
                    }
                    return;
                }

                if (needSend)
                {
                    _sendThread = new Thread(SendThread) { Priority = ThreadPriority.Highest };
                    _sendThread.Start();
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
