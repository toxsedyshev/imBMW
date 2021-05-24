using System;
using Microsoft.SPOT;
using System.Collections;
using System.Threading;

namespace imBMW.Tools
{
    public class QueueThreadWorker : Queue
    {
        public delegate void ProcessItem(object item);

        public int MaxSleepBeforeSuspend = 0;

        Thread queueThread;
        ProcessItem processItem;
        object lockObj = new object();
        ulong added, processed, suspended, resumed;

        public QueueThreadWorker(ProcessItem processItem)
        {
            if (processItem == null)
            {
                throw new ArgumentException("processItem is null");
            }
            this.processItem = processItem;
            queueThread = new Thread(queueWorker);
            queueThread.Priority = ThreadPriority.AboveNormal;
            queueThread.Start();
        }

        void queueWorker()
        {
            object m = null;
            int sleep = 0;

            while (true)
            {
                //lock (lockObj)
                //{
                if (Count > 0)
                {
                    m = Dequeue();
                }
                else
                {
                    //m = null;
                //}
                //}
                //if (m == null)
                //{
                    if (sleep < MaxSleepBeforeSuspend)
                    {
                        Thread.Sleep(++sleep);
                        continue;
                    }
                    sleep = 0;
                    suspended++;
                    Thread.CurrentThread.Suspend();
                    continue;
                }
                try
                {
                    processItem(m);
                    processed++;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "while processing QueueThreadWorker item '" + m.ToString() + "'");
                }
            }
        }

        public override void Enqueue(object item)
        {
            if (item == null)
            {
                throw new ArgumentException("item is null");
            }
            lock (lockObj)
            {
                base.Enqueue(item);
                added++;
                CheckRunning();
            }
        }

        public void EnqueueArray(params object[] items)
        {
            if (items == null)
            {
                throw new ArgumentException("items is null");
            }
            lock (lockObj)
            {
                foreach (object item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    base.Enqueue(item);
                    added++;
                }
                CheckRunning();
            }
        }

        public void CheckRunning()
        {
            /**
             * Warning! Current item may be added to suspended queue and will be processed only on next Enqueue().
             * Tried AutoResetEvent instead of Suspend/Resume but no success because of strange slowness.
             */
            // TODO Check ResetEvent on LDR and LED
            if (queueThread.ThreadState == ThreadState.Suspended || queueThread.ThreadState == ThreadState.SuspendRequested)
            {
                queueThread.Resume();
                resumed++;
            }
        }
    }
}
