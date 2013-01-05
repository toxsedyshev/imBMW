using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus.Devices
{
    static class iPodChanger
    {
        static OutputPort iPod;

        static Thread announceThread;

        #region Messages

        static Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        static Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x01);
        static Message MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, 0x39, 0x00, 0x09, 0x00, 0x3F, 0x00, 0x01, 0x01);

        static byte[] DataPollRequest = new byte[] { 0x01 };
        static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };

        #endregion

        public static void Init(Cpu.Pin headsetControl)
        {
            iPod = new OutputPort(headsetControl, true);

            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, processMessage);

            announceThread = new Thread(announce);
            announceThread.Start();
        }

        static void processMessage(Message m)
        {
            if (m.Data.Compare(MessageAnnounce.Data))
            {
                if (announceThread.ThreadState == ThreadState.Suspended)
                {
                    announceThread.Resume();
                }
                Debug.Print("iBus activated");
            }
            else if (m.Data.Compare(DataPollRequest))
            {
                /*if (announceThread.ThreadState != ThreadState.Suspended)
                {
                    announceThread.Suspend();
                }*/

                Manager.EnqueueMessage(MessagePollResponse);

                //Thread.Sleep(50);
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);

                Debug.Print("Radio polled");
            }
            else if (m.Data.Compare(DataCurrentDiskTrackRequest))
            {
                Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                Debug.Print("Radio requested disk&track");
            }
            else if(m.SourceDevice == DeviceAddress.Radio)
            {
                Debug.Print(m.PrettyDump);
            }
        }

        static void announce()
        {
            while (true)
            {
                Manager.EnqueueMessage(MessageAnnounce);

                Thread.Sleep(50);
                Manager.EnqueueMessage(MessagePollResponse);

                Thread.Sleep(30000);
            }
        }
    }
}
