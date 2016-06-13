using System;

namespace ImageCompLibWin.Threading
{
    public class SimpleLock : IDisposable
    {
        public SimpleLock(ISimpleLocker locker)
        {
            locker.Mutex.WaitOne();
            Locker = locker;
        }

        public SimpleLock(ISimpleLocker locker, int millisecondsTimeout)
        {
            if (locker.Mutex.WaitOne(millisecondsTimeout))
            {
                Locker = locker;
            }
        }

        public ISimpleLocker Locker { get; private set; }

        public void Unlock()
        {
            if (Locker != null)
            {
                Locker.Mutex.ReleaseMutex();
                Locker = null;
            }
        }

        public void Dispose()
        {
            Unlock();
        }
    }
}
