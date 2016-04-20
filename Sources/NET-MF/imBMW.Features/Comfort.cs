using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using System.Threading;
using imBMW.Tools;
using imBMW.iBus;

namespace imBMW.Features
{
    public static class Comfort
    {
        #region Enums
        enum Command
        {
            FullCloseWindows,
            FullOpenWindows
        }
        #endregion

        static QueueThreadWorker commands;

        static bool needLockDoors = false;
        static bool needUnlockDoors = false;
        static bool needComfortClose = false;

        static Comfort()
        {
            commands = new QueueThreadWorker(ProcessCommand);

            InstrumentClusterElectronics.SpeedRPMChanged += (e) =>
            {
                if (needLockDoors && e.Speed > DoorsLockSpeed)
                {
                    if (AutoLockDoors)
                    {
                        BodyModule.LockDoors();
                    }
                    needLockDoors = false;
                    needUnlockDoors = true;
                }
                if (e.Speed == 0)
                {
                    needLockDoors = true;
                }
            };
            InstrumentClusterElectronics.IgnitionStateChanged += (e) =>
            {
                if (!needComfortClose 
                    && e.CurrentIgnitionState != IgnitionState.Off 
                    && e.PreviousIgnitionState == IgnitionState.Off)
                {
                    needComfortClose = true;
                }
                if (needUnlockDoors && e.CurrentIgnitionState == IgnitionState.Off)
                {
                    if (AutoUnlockDoors)
                    {
                        BodyModule.UnlockDoors();
                    }
                    needUnlockDoors = false;
                    needLockDoors = true;
                }
            };
            BodyModule.RemoteKeyButtonPressed += (e) =>
            {
                if (e.Button == RemoteKeyButton.Lock && needComfortClose)
                {
                    needComfortClose = false;
                    if (AutoCloseWindows)
                    {
                        commands.Enqueue(Command.FullCloseWindows);
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
                    for (int i = 0; i < 3; i++)
                    {
                        Manager.EnqueueMessage(BodyModule.MessageCloseWindowDriverFront);
                        Thread.Sleep(750);
                        Manager.EnqueueMessage(BodyModule.MessageCloseWindowPassengerFront);
                        Thread.Sleep(750);
                        Manager.EnqueueMessage(BodyModule.MessageCloseWindowDriverRear);
                        Thread.Sleep(750);
                        Manager.EnqueueMessage(BodyModule.MessageCloseWindowPassengerRear);
                        Thread.Sleep(750);
                    }
                    break;
                case Command.FullOpenWindows:
                    for (int i = 0; i < 3; i++)
                    {
                        Manager.EnqueueMessage(BodyModule.MessageOpenWindowDriverFront);
                        Thread.Sleep(750);
                        Manager.EnqueueMessage(BodyModule.MessageOpenWindowPassengerFront);
                        Thread.Sleep(750);
                        Manager.EnqueueMessage(BodyModule.MessageOpenWindowDriverRear);
                        Thread.Sleep(750);
                        Manager.EnqueueMessage(BodyModule.MessageOpenWindowPassengerRear);
                        Thread.Sleep(750);
                    }
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
                return needComfortClose;
            }
            set
            {
                needComfortClose = value;
            }
        }

        /// <summary>
        /// Set is all comfort features (auto open/close doors, windows, sunroof, mirrors) enabled
        /// </summary>
        public static bool AllFeaturesEnabled
        {
            set
            {
                Features.Comfort.AutoLockDoors = value;
                Features.Comfort.AutoUnlockDoors = value;
                Features.Comfort.AutoCloseWindows = value;
                Features.Comfort.AutoCloseSunroof = value;
                Features.Comfort.AutoFoldMirrors = value;
                Features.Comfort.AutoUnfoldMirrors = value;
            }
        }
    }
}
