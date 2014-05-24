using System;
using System.Collections;
using System.Threading;

namespace imBMW.Tools
{
    class QueueThreadWorker : Queue
    {
        public delegate void ProcessItem(object item);

        readonly Thread _queueThread;
        readonly ProcessItem _processItem;
        readonly object _lockObj = new object();

        public QueueThreadWorker(ProcessItem processItem)
        {
            if (processItem == null)
            {
                throw new ArgumentException("processItem is null");
            }
            _processItem = processItem;
            _queueThread = new Thread(QueueWorker) {Priority = ThreadPriority.AboveNormal};
            _queueThread.Start();
        }

        void QueueWorker()
        {
            while (true)
            {
                object m;
                lock (_lockObj)
                {
                    m = Count > 0 ? Dequeue() : null;
                }
                if (m == null)
                {
                    Thread.CurrentThread.Suspend();
                    continue;
                }
                try
                {
                    _processItem(m);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "while processing QueueThreadWorker item '" + m + "'");
                }
            }
        }

        public override void Enqueue(object item)
        {
            if (item == null)
            {
                throw new ArgumentException("item is null");
            }
            lock (_lockObj)
            {
                base.Enqueue(item);
                CheckRunning();
            }
        }

        public void EnqueueArray(params object[] items)
        {
            if (items == null)
            {
                throw new ArgumentException("items is null");
            }
            lock (_lockObj)
            {
                foreach (object item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    base.Enqueue(item);
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
            if (_queueThread.ThreadState == ThreadState.Suspended || _queueThread.ThreadState == ThreadState.SuspendRequested)
            {
                _queueThread.Resume();
            }
        }
    }
}
