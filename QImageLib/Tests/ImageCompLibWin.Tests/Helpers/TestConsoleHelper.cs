using System;
using System.Threading;

namespace ImageCompLibWin.Tests.Helpers
{
    public static class TestConsoleHelper
    {
        private static Mutex _printMutex = new Mutex();

        public static void Print(string s)
        {
            _printMutex.WaitOne();
            Console.WriteLine(s);
            _printMutex.ReleaseMutex();
        }
    }
}
