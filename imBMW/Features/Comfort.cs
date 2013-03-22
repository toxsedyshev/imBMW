using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;

namespace imBMW.Features
{
    public static class Comfort
    {
        static bool needLockDoors = true;
        static bool needUnlockDoors = false;

        static Comfort()
        {
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
                if (e.Button == RemoteKeyButton.Lock)
                {
                    if (AutoCloseWindows)
                    {
                        // TODO Fix windows closing: current commands close them just a half
                        BodyModule.CloseWindows();
                        BodyModule.CloseWindows();
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
        /// Are all comfort features (auto open/close doors, windows, sunroof, mirrors) enabled?
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
