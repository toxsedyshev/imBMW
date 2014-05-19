using imBMW.iBus.Devices.Real;
using System.Threading;
using imBMW.Tools;

namespace imBMW.Features
{
    public static class Comfort
    {
        #region Enums
        enum Command
        {
            FullCloseWindows,
            FullOpenWindows,
			UnlockDoors
        }
        #endregion

        static readonly QueueThreadWorker Commands;

        static bool _needLockDoors = true;
        static bool _needUnlockDoors;
        static bool _needComfortClose = true;

        static Comfort()
        {
            Commands = new QueueThreadWorker(ProcessCommand);

            InstrumentClusterElectronics.SpeedRPMChanged += e =>
            {
                if (_needLockDoors && e.Speed > DoorsLockSpeed)
                {
                    if (AutoLockDoors)
                    {
                        BodyModule.LockDoors();
                    }
                    _needLockDoors = false;
                    _needUnlockDoors = true;
                }
                if (e.Speed == 0)
                {
                    _needLockDoors = true;
                }
            };
            InstrumentClusterElectronics.IgnitionStateChanged += e =>
            {
                if (!_needComfortClose 
                    && e.CurrentIgnitionState != IgnitionState.Off 
                    && e.PreviousIgnitionState == IgnitionState.Off)
                {
                    _needComfortClose = true;
                }
                if (_needUnlockDoors && e.CurrentIgnitionState == IgnitionState.Off)
                {
                    if (AutoUnlockDoors)
                    {
						Commands.Enqueue(Command.UnlockDoors);
                    }
                    _needUnlockDoors = false;
                    _needLockDoors = true;
                }
            };
            BodyModule.RemoteKeyButtonPressed += e =>
            {
                if (e.Button == RemoteKeyButton.Lock && _needComfortClose)
                {
                    _needComfortClose = false;
                    if (AutoCloseWindows)
                    {
                        Commands.Enqueue(Command.FullCloseWindows);
                    }
                    if (AutoCloseSunroof)
                    {
                        BodyModule.CloseSunroof();
                    }
                    if (AutoFoldMirrors)
                    {
                        BodyModule.FoldMirrors();
                    }
                }
                if (e.Button == RemoteKeyButton.Unlock)
                {
                    if (AutoUnfoldMirrors)
                    {
                        BodyModule.UnfoldMirrors();
                    }
                }
            };
        }

        private static void ProcessCommand(object o)
        {
            var c = (Command)o;
            switch (c)
            {
                // TODO Fix windows closing: current commands close them just by half
                case Command.FullCloseWindows:
                    BodyModule.CloseWindows();
                    Thread.Sleep(3000);
                    BodyModule.CloseWindows();
                    Thread.Sleep(3000);
                    BodyModule.CloseWindows();
                    break;
                case Command.FullOpenWindows:
                    BodyModule.OpenWindows();
                    Thread.Sleep(3000);
                    BodyModule.OpenWindows();
                    Thread.Sleep(3000);
                    BodyModule.OpenWindows();
                    break;
				case Command.UnlockDoors:
					BodyModule.UnlockDoors();
					Thread.Sleep(1000);
					BodyModule.UnlockDoors();
		            break;
            }
        }

        /// <summary>
        /// Lock doors on specified speed (km/h)
        /// </summary>
        public static uint DoorsLockSpeed = 5;

        /// <summary>
        /// Lock doors on <see cref="DoorsLockSpeed"/> speed reached
        /// </summary>
        public static bool AutoLockDoors = false;

        /// <summary>
        /// Unlock doors on ignition off
        /// </summary>
        public static bool AutoUnlockDoors = false;

        /// <summary>
        /// Close windows on remote key "lock" button
        /// </summary>
        public static bool AutoCloseWindows = false;

        /// <summary>
        /// Close sunroof on remote key "lock" button
        /// </summary>
        public static bool AutoCloseSunroof = false;

        /// <summary>
        /// Fold mirrors on remote key "lock" button
        /// </summary>
        public static bool AutoFoldMirrors = false;

        /// <summary>
        /// Unfold mirrors on remote key "unlock" button
        /// </summary>
        public static bool AutoUnfoldMirrors = false;

        /// <summary>
        /// Is comfort close enabled till next ignition on.
        /// If disabled, it will be enabled on ignition on.
        /// </summary>
        public static bool NextComfortCloseEnabled
        {
            get
            {
                return _needComfortClose;
            }
            set
            {
                _needComfortClose = value;
            }
        }

        /// <summary>
        /// Set is all comfort features (auto open/close doors, windows, sunroof, mirrors) enabled
        /// </summary>
        public static bool AllFeaturesEnabled
        {
            set
            {
                AutoLockDoors = value;
                AutoUnlockDoors = value;
                AutoCloseWindows = value;
                AutoCloseSunroof = value;
                AutoFoldMirrors = value;
                AutoUnfoldMirrors = value;
            }
        }
    }
}
