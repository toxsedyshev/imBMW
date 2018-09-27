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
        static bool lastEmulationFailed;
        static object emulatorLock = new object();

        //static CanMessage canEmulationSeatFront = new CanMessage(0x0DA, new byte[] { 0x01, 0x00, 0xC0, 0xFF });
        //static CanMessage canEmulationEngineStop = new CanMessage(0x5A9, new byte[] { 0x30, 0x06, 0x00, 0x70, 0x17, 0xF1, 0x62, 0x03 });
        //static CanMessage canEmulationEngineStart = new CanMessage(0x38E, new byte[] { 0xF4, 0x01 });
        //static CanMessage canEmulationEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x40, 0x21, 0x8F, 0xFE });
        //static CanMessage canEmulationTransmissionD = new CanMessage(0x1D2, new byte[] { 0x78, 0x0C, 0x8B, 0x1C, 0xF0 });
        //static CanMessage canEmulationTransmissionP = new CanMessage(0x1D2, new byte[] { 0xE1, 0x0C, 0x8B, 0x1C, 0xF0 });
        //static CanMessage emulateEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x40, 0x21, 0x8F, 0xFE });

        static CanMessage messageKeepAlive = new CanMessage(0x130, new byte[] { 0x45, 0xFE, 0xFC, 0xFF, 0xFF }); // every 100ms
        static CanMessage messageEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x41, 0x39, 0xBF, 0xB0 }); // every 100ms, last byte is incremented by 0x11 (from (00)B0 to (01)9E)
        static CanMessage messageEmulateIHKA = new CanMessage(0x4F8, new byte[] { 0x00, 0x42, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }); // every 700ms. Some IHKA message?
        static CanMessage messageLightDimmer = new CanMessage(0x202, new byte[] { 0xFC, 0xFF, 0x00, 0x00, 0x00 }); // every 1'000ms
        static CanMessage messageEmulationUnknown5 = new CanMessage(0x2CC, new byte[] { 0x80, 0x80, 0xF6 }); // every 10'000ms

        public static void Init()
        {
            //CanAdapter.Current.MessageReceived += CanMessageReceived;
        }

        static void EmulatorWorker()
        {
            int time = int.MaxValue - 2; //1;
            while (emulatorStarted)
            {
                var can = CanAdapter.Current;
                if (can.SendMessage(messageKeepAlive))
                {
                    var engineMessage = messageEngineRunning;
                    var counter = engineMessage.Data[4];
                    counter += 0x11;
                    if (counter == 0xAF)
                    {
                        counter = 0xB0;
                    }
                    engineMessage.Data[4] = counter;
                    can.SendMessage(engineMessage);

                    if (time % 10 == 0)
                    {
                        can.SendMessage(messageLightDimmer);
                    }

                    if (time % 100 == 0)
                    {
                        can.SendMessage(messageEmulationUnknown5);
                    }

                    if (time % 7 == 0)
                    {
                        can.SendMessage(messageEmulateIHKA);
                    }

                    lastEmulationFailed = false;
                    time++;
                    Thread.Sleep(80); // 100ms - 20ms for processing
                }
                else
                {
                    Thread.Sleep(lastEmulationFailed ? 1000 : 80); // slow down on second unsuccessful attempt
                    lastEmulationFailed = true;
                }
            }
        }

        //private static void CanMessageReceived(CanAdapter can, CanMessage message)
        //{
        //    if (message.Compare(emulateEngineRunning))
        //    {
        //        Logger.Info("Emulated", "CAN");
        //    }
        //}

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
                emulatorThread.Start();
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
                    if (emulatorThread.ThreadState == ThreadState.Running)
                    {
                        emulatorThread.Abort();
                    }
                    emulatorThread = null;
                }
            }
        }
    }
}
