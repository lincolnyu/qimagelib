using System.Threading;

namespace ImageCompLibWin.Threading
{
    public interface ISimpleLocker
    {
        Mutex Mutex { get; }
    }
}
