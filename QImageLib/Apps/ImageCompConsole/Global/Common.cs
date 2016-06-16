using System;

namespace ImageCompConsole.Global
{
    public static class Common
    {
        public const int LineLen = 118;

        private static DateTime _startTime;

        public static void ResetInPlaceWriting()
        {
            ConsoleHelper.ResetInPlaceWriting(LineLen);
        }

        public static void StartProgress()
        {
            _startTime = DateTime.UtcNow;
        }

        public static void PrintProgress(long tasks, long total, bool forcePrint = false)
        {
            if (forcePrint || ConsoleHelper.CanFreqPrint())
            {
                var curr = DateTime.UtcNow;
                var elapsed = curr - _startTime;
                var estMins = elapsed.TotalMinutes * total / tasks;
                var remain = TimeSpan.FromMinutes(estMins) - elapsed;
                var elapsedstr = elapsed.ToString(@"d\.hh\:mm\:ss");
                var remainstr = remain.ToString(@"d\.hh\:mm");

                var perc = tasks * 100 / total;
                $"{tasks}/{total} ({perc}%) completed. {elapsedstr} elapsed, est. {remainstr} remaining.".InPlaceWriteToConsole();
                ConsoleHelper.UpdateLastPrintTime();
            }
        }
    }
}
