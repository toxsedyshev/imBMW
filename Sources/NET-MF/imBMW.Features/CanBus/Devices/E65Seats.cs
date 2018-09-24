using System;
using Microsoft.SPOT;
using System.Threading;
using imBMW.Features.CanBus.Adapters;
using imBMW.Tools;

namespace imBMW.Features.CanBus.Devices
{
    public static class E65Seats
    {
        static Thread emulatorThread;
        static bool emulatorStarted;
        static object emulatorLock = new object();

        //static CanMessage canEmulationSeatFront = new CanMessage(0x0DA, new byte[] { 0x01, 0x00, 0xC0, 0xFF });
        //static CanMessage canEmulationEngineStop = new CanMessage(0x5A9, new byte[] { 0x30, 0x06, 0x00, 0x70, 0x17, 0xF1, 0x62, 0x03 });
        //static CanMessage canEmulationEngineStart = new CanMessage(0x38E, new byte[] { 0xF4, 0x01 });
        //static CanMessage canEmulationEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x40, 0x21, 0x8F, 0xFE });
        //static CanMessage canEmulationTransmissionD = new CanMessage(0x1D2, new byte[] { 0x78, 0x0C, 0x8B, 0x1C, 0xF0 });
        //static CanMessage canEmulationTransmissionP = new CanMessage(0x1D2, new byte[] { 0xE1, 0x0C, 0x8B, 0x1C, 0xF0 });
        static CanMessage emulateEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x40, 0x21, 0x8F, 0xFE });

        static E65Seats()
        {
            CanAdapter.Current.MessageReceived += CanMessageReceived;
        }

        private static void CanMessageReceived(CanAdapter can, CanMessage message)
        {
            if (message.Compare(emulateEngineRunning))
            {
                Logger.Info("Emulated", "CAN");
            }
        }

        public static void StartEmulator()
        {
            lock (emulatorLock)
            {
                if (emulatorStarted)
                {
                    return;
                }
                emulatorStarted = true;
                emulatorThread = new Thread(EmulatorWorker);
            }
        }

        public static void StopEmulator()
        {
            lock (emulatorLock)
            {
                if (!emulatorStarted)
                {
                    return;
                }
                emulatorStarted = false;
                if (emulatorThread != null)
                {
                    emulatorThread.Abort();
                    emulatorThread = null;
                }
            }
        }

        static void EmulatorWorker()
        {
            while (emulatorStarted)
            {
                CanAdapter.Current.SendMessage(emulateEngineRunning);
                Thread.Sleep(50);
            }
        }
    }
}
