using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageCompLibWin.Tasking;
using static ImageCompLibWin.Tests.Helpers.TestConsoleHelper;

namespace ImageCompLibWin.Tests
{
    [TestClass]
    public class ImageTaskTests
    {
        public class MockImage : IResource
        {
            public int Size { get; set; }

            public int HoldCount
            {
                get; set;
            }

            public int ReferenceCount
            {
                get; set;
            }

            public bool IsEngaged
            {
                get; set;
            }
        }

        public class MockCompTask : Tasking.Task
        {
            private ICollection<IResource> _requiredREsources;

            public MockCompTask(TaskManager manager, MockImage image1, MockImage image2) : base(manager)
            {
                _requiredREsources = new MockImage[] {
                    image1,
                    image2
                };

                Image1 = image1;
                Image2 = image2;
            }

            public override ICollection<IResource> RequiredResources => _requiredREsources;

            public MockImage Image1 { get; }
            public MockImage Image2 { get; }

            public int EstWorkloadMs { get; }

            protected override void Perform()
            {
                var size1 = Image1.Size;
                var size2 = Image2.Size;
                SimWork(size1, size2);
            }

            public void SeqRun()
            {
                Perform();
            }
        }

        const int MaxImageEdgeSize = 6000;
        const int MinImageEdgeSize = 64;
        public const int StringentQuota = 1024 * 1024 * 36 * 2;
        public const int GenerousQuota = 1024 * 1024 * 1024;

        public static Random Rand = new Random(123);

        [TestMethod]
        public void ConcurrencyStringentTest()
        {
            const int n = 16;
            ConcurrencyTest(n, StringentQuota);
        }

        [TestMethod]
        public void ConcurrencyEasyTest()
        {
            const int n = 16;
            ConcurrencyTest(n, GenerousQuota);
        }

        public static int GetNumTasks(int numImages)
        {
            return (numImages - 1) * numImages / 2;
        }

        private void ConcurrencyTest(int numImages, int quota)
        {
            var manager = new TaskManager(quota);
            manager.Start();
            var imglist = GenerateImages(numImages).ToList();
            Parallel.For(0, numImages - 2, i =>
              {
                  Parallel.For(i + 1, numImages - 1, j =>
                      {
                          var image1 = imglist[i];
                          var image2 = imglist[j];
                          var task = new MockCompTask(manager, image1, image2);
                          task.Run();
                      });
              });
            Assert.IsTrue(manager.Used <= manager.Quota);
            Assert.AreEqual(manager.CachedResources.Sum(x => x.Size), manager.Used);
            manager.Stop();
            Assert.AreEqual(0, manager.Used);
        }

        public static void ConcurrencyTestConsole(IList<MockImage> images, int quota)
        {
            var manager = new TaskManager(quota);
            manager.Start();
            var numImages = images.Count;
            var totalTasks = GetNumTasks(numImages);
            var completed = 0;
            Parallel.For(0, numImages - 1, i =>
            {
                var image1 = images[i];
                Parallel.For(i + 1, numImages, j =>
                  {
                      var image2 = images[j];
                      var task = new MockCompTask(manager, image1, image2);
                      Print($"Starting {i},{j} to be taking ~ {task.EstWorkloadMs / 1000.0:0.00} secs");
                      task.Run();
                      Interlocked.Increment(ref completed);
                      Print($"Completed {i},{j} ({completed}/{totalTasks})");
                  });
            });
            manager.Stop();
        }

        public static void SequentialTestConsole(IList<MockImage> images)
        {
            var numImages = images.Count;
            var totalTasks = GetNumTasks(numImages);
            var completed = 0;
            for (var i = 0; i < numImages - 1; i++)
            {
                var image1 = images[i];
                for (var j = i + 1; j < numImages; j++)
                {
                    var image2 = images[j];
                    var task = new MockCompTask(null, image1, image2);
                    Print($"Starting {i},{j} to be taking ~ {task.EstWorkloadMs / 1000.0:0.00} secs");
                    task.SeqRun();
                    Print($"Completed {i},{j} ({++completed}/{totalTasks})");
                }
            }
        }

        public static IEnumerable<MockImage> GenerateImages(int numImages)
        {
            for (var i = 0; i < numImages; i++)
            {
                var imageWidth = Rand.Next(MinImageEdgeSize, MaxImageEdgeSize);
                var imageHeight = Rand.Next(MinImageEdgeSize, MaxImageEdgeSize);
                var imageSize = imageWidth * imageHeight;
                var mockImage = new MockImage
                {
                    Size = imageSize,
                };
                yield return mockImage;
            }
        }

        private static void SimWork(int size1, int size2)
        {
            var startTime = DateTime.UtcNow;

            int loadTimeMs = (size1 + size2)/40000;
            Thread.Sleep(loadTimeMs);

            var sum = 0.0;
            var max = Math.Max(size1, size2);
            for (var i = 0; i < size1; i++)
            {
                var d1 = Rand.NextDouble();
                var d2 = Rand.NextDouble();
                var d = d1 - d2;
                var dd = d * d;
                sum += dd;
            }
        }
    }
}
