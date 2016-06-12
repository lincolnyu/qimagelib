using System;

namespace ImageCompConsole
{
    public static class ConsoleHelper
    {
        public const int DefaultLineLength = 78;

        public readonly static TimeSpan DefaultMinPrintInterval = TimeSpan.FromMilliseconds(1000);

        public static int LastInPlaceWriteLen { get; private set; }

        public static int LineLength { get; set; } = DefaultLineLength;
        
        public static DateTime LastPrintTime { get; private set; }

        public static TimeSpan MinPrintInterval { get; set; } = DefaultMinPrintInterval;

        public static void ResetInPlaceWriting()
        {
            LastInPlaceWriteLen = 0;
        }

        public static void ResetInPlaceWriting(int lineLength)
        {
            LineLength = lineLength;
            LastInPlaceWriteLen = 0;
        }

        public static void InPlaceWriteToConsole(this string s)
        {
            if (s.Length > LineLength)
            {
                s = s.Substring(0, LineLength);
            }
            if (s.Length < LastInPlaceWriteLen)
            {
                var blank = new string(' ', LastInPlaceWriteLen - s.Length);
                s += blank;
            }
            Console.Write($"\r{s}");
            LastInPlaceWriteLen = s.Length;
            UpdateLastPrintTime();
        }

        public static string GetSwitchValue(this string[] args, string sw)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var x = args[i];
                if (x == sw)
                {
                    if (i+1 < args.Length)
                    {
                        var t = args[i + 1];
                        if (t.StartsWith("-"))
                        {
                            return "";
                        }
                        return t;
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            return null;
        }

        public static void UpdateLastPrintTime()
        {
            LastPrintTime = DateTime.UtcNow;
        }

        public static bool CanFreqPrint()
        {
            var dur = DateTime.UtcNow - LastPrintTime;
            return dur >= MinPrintInterval;
        }
    }
}
