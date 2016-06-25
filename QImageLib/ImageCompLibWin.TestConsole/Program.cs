using System;
using System.Collections.Generic;
using System.Linq;
using static ImageCompLibWin.Tests.ImageTaskTests;
using QLogger.Logging;

namespace ImageCompLibWin.TestConsole
{
    class Program
    {
        private static Stopwatch _stopwatch = new Stopwatch();

        static void ParallelTest(IList<MockImage> images)
        {
            var numImages = images.Count;
            Console.WriteLine("Running parallel...");
            using (_stopwatch.Create((log, st, et) =>
                Console.WriteLine($"Parallel ran {numImages} for { (et - st).TotalSeconds:0.00} secs")))
            {
                ConcurrencyTestConsole(images, StringentQuota);
            }
        }

        private static void SequentialTest(List<MockImage> images)
        {
            var numImages = images.Count;
            Console.WriteLine("Running sequential...");
            using (_stopwatch.Create((log, st, et) =>
              Console.WriteLine($"Sequential ran {numImages} for { (et - st).TotalSeconds:0.00} secs")))
            {
                SequentialTestConsole(images);
            }
        }

        static void Main(string[] args)
        {
            const int numImages = 16;
            var images = GenerateImages(numImages).ToList();
            ParallelTest(images);
            SequentialTest(images);
        }
    }
}
