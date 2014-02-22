using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using System.Threading;
using imBMW.Tools;

namespace imBMW.Features
{
    public static class Comfort
    {
        #region Enums

        private enum Command
        {
            FullCloseWindows,
            FullOpenWindows
        }

        #endregion

        #region Private static fields
        
        static readonly QueueThreadWorker Commands;

        static bool _needLockDoors = true;
        static bool _needUnlockDoors;

        #endregion

        #region Public static properties

        /// <summary>
        /// Lock doors on specified speed (km/h)
        /// </summary>
        public static uint DoorsLockSpeed { get; set; }

        /// <summary>
        /// Lock doors on <see cref="DoorsLockSpeed"/> speed reached
        /// </summary>
        public static bool AutoLockDoors { get; set; }

        /// <summary>
        /// Unlock doors on ignition off
        /// </summary>
        public static bool AutoUnlockDoors { get; set; }

        /// <summary>
        /// Close windows on remote key "lock" button
        /// </summary>
        public static bool AutoCloseWindows { get; set; }

        /// <summary>
        /// Close sunroof on remote key "lock" button
        /// </summary>
        public static bool AutoCloseSunroof { get; set; }

        /// <summary>
        /// Fold mirrors on remote key "lock" button
        /// </summary>
        public static bool AutoFoldMirrors { get; set; }

        /// <summary>
        /// Unfold mirrors on remote key "unlock" button
        /// </summary>
        public static bool AutoUnfoldMirrors { get; set; }

        /// <summary>
        /// Send seat memory position auto when appropriate key is inserted
        /// </summary>
        public static bool AutoApplySeatMemory { get; set; }

        /// <summary>
        /// Is comfort close enabled till next ignition on.
        /// If disabled, it will be enabled on ignition on.
        /// </summary>
        public static bool NextComfortCloseEnabled { get; set; }

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
                AutoApplySeatMemory = value;
            }
        }

        #endregion

        #region Static constructor

        static Comfort()
        {
            DoorsLockSpeed = 5;
            NextComfortCloseEnabled = true;
            Commands = new QueueThreadWorker(ProcessCommand);

            Immobiliser.KeyInserted += args =>
                {
                    if (AutoApplySeatMemory)
                    {
                        switch (args.KeyNumber)
                        {
                            case 1:
                                SeatMemory.SeatPosition = SeatMemoryPosition.Position1;
                                break;
                            case 2:
                                SeatMemory.SeatPosition = SeatMemoryPosition.Position2;
                                break;
                            case 3:
                                SeatMemory.SeatPosition = SeatMemoryPosition.Position3;
                                break;
                        }
                    }
                };
            InstrumentClusterElectronics.CarDataChanged += e =>
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
                if (!NextComfortCloseEnabled
                    && e.CurrentIgnitionState != IgnitionState.Off
                    && e.PreviousIgnitionState == IgnitionState.Off)
                {
                    NextComfortCloseEnabled = true;
                }
                if (_needUnlockDoors && e.CurrentIgnitionState == IgnitionState.Off)
                {
                    if (AutoUnlockDoors)
                    {
                        BodyModule.UnlockDoors();
                    }
                    _needUnlockDoors = false;
                    _needLockDoors = true;
                }
            };
            BodyModule.RemoteKeyButtonPressed += e =>
            {
                if (e.Button == RemoteKeyButton.Lock && NextComfortCloseEnabled)
                {
                    NextComfortCloseEnabled = false;
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

        #endregion

        #region Private static methods
        
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
            }
        }

        #endregion
    }
}
