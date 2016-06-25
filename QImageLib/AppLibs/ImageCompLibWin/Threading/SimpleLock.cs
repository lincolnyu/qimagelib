using System;
using System.Threading;

namespace ImageCompLibWin.Threading
{
    public class SimpleLock : IDisposable
    {
        public SimpleLock(Mutex mutex)
        {
            Mutex = mutex;
        }

        ~SimpleLock()
        {
            Unlock();
        }

        public Mutex Mutex { get; private set; }

        private void Unlock()
        {
            if (Mutex != null)
            {
                Mutex.ReleaseMutex();
                Mutex = null;
            }
        }

        public void Dispose()
        {
            Unlock();
            GC.SuppressFinalize(this);
        }
    }
}
