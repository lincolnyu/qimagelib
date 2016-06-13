using System.Threading;

namespace ImageCompLibWin.Threading
{
    public class SimpleLocker : ISimpleLocker
    {
        public SimpleLock Lock()
        {
            return new SimpleLock(this);
        }


        public Mutex Mutex { get; } = new Mutex();
    }
}
