using System;
using Microsoft.SPOT;
using System.Collections;
using System.Threading;

namespace imBMW.Tools
{
    class QueueThreadWorker : Queue
    {
        public delegate void ProcessItem(object item);

        Thread queueThread;
        ProcessItem processItem;
        ManualResetEvent wait = new ManualResetEvent(true);
        object lockObj = new object();

        public QueueThreadWorker(ProcessItem processItem)
        {
            if (processItem == null)
            {
                throw new ArgumentException("processItem is null");
            }
            this.processItem = processItem;
            queueThread = new Thread(queueWorker);
            queueThread.Start();
        }

        void queueWorker()
        {
            object m;
            while (true)
            {
                wait.WaitOne();

                lock (lockObj)
                {
                    if (Count > 0)
                    {
                        m = Dequeue();
                    }
                    else
                    {
                        m = null;
                    }
                }
                if (m == null)
                {
                    wait.Reset();
                }
                else
                {
                    processItem(m);
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
                wait.Set();
            }
        }
    }
}
