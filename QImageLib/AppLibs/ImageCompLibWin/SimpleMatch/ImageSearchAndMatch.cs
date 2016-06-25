using System.Collections.Generic;
using System.Linq;
using QImageLib.Matcher;
using System.Threading.Tasks;
using ImageCompLibWin.Data;

namespace ImageCompLibWin.SimpleMatch
{
    public static class ImageSearchAndMatch
    {
        public const double DefaultAspRatioThr = 1.05;

        public delegate void ProgressReport(int i, int j);

        public delegate void ParallelProgressReport();

        private class HastyImage
        {
            public HastyImage(ImageProxy image)
            {
                WrappedImage = image;
            }

            public ImageProxy WrappedImage { get; }

            public bool Excluded { get; set; }
        }

        public static IEnumerable<SimpleImageMatch> SimpleSearchAndMatchImages(this IList<ImageProxy> images, ProgressReport progress = null,
            double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr)
        {
            bool? histo = null;
            for (var i = 0; i < images.Count - 1; i++)
            {
                var image1 = images[i];
                var r1 = image1.AbsAspRatio;
                for (var j = i + 1; j < images.Count; j++)
                {
                    var image2 = images[j];
                    var r2 = image2.AbsAspRatio;
                    if (r2 > r1 * aspThr)
                    {
                        progress?.Invoke(i, j);
                        break;
                    }

                    var mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                    if (histo != true && mse != null)
                    {
                        mse = TryToFineCompare(image1, image2, mseThr);
                    }
                    if (mse != null)
                    {
                        yield return new SimpleImageMatch(image1, image2, mse.Value);
                    }

                    progress?.Invoke(i, j);
                }
                image1.IsEngaged = false;
            }
        }

        public static IEnumerable<SimpleImageMatch> SimpleSearchAndMatchImagesHasty(this IEnumerable<ImageProxy> images, ProgressReport progress = null,
            double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr)
        {
            var hastyList = images.Select(x => new HastyImage(x)).ToList();
            for (var i = 0; i < hastyList.Count - 1; i++)
            {
                var hasty1 = hastyList[i];
                if (hasty1.Excluded) continue;
                var image1 = hasty1.WrappedImage;
                var r1 = image1.AbsAspRatio;
                for (var j = i + 1; j < hastyList.Count; j++)
                {
                    var hasty2 = hastyList[j];
                    if (hasty2.Excluded) continue;
                    var image2 = hasty2.WrappedImage;
                    var r2 = image2.AbsAspRatio;
                    if (r2 > r1 * aspThr)
                    {
                        progress?.Invoke(i, j);
                        break;
                    }
                    
                    var mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                    if (mse != null)
                    {
                        mse = TryToFineCompare(image1, image2, mseThr);
                    }
                    if (mse != null)
                    {
                        hasty1.Excluded = true;
                        hasty2.Excluded = true;
                        yield return new SimpleImageMatch(image1, image2, mse.Value);
                        image2.IsEngaged = false;
                    }
                    progress?.Invoke(i, j);
                }
                image1.IsEngaged = false;
            }
        }

        public static IList<SimpleImageMatch> SimpleSearchAndMatchImagesParallel(this IList<ImageProxy> images, ParallelProgressReport progress = null,
          double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr)
        {
            var result = new List<SimpleImageMatch>();
            var manager = images.FirstOrDefault()?.ImageManager;
            if (manager == null) return result;
            manager.TaskManager.Start();
            Parallel.For(0, images.Count - 1, (int i) =>
            {
                var image1 = images[i];
                var r1 = image1.AbsAspRatio;

                Parallel.For(i + 1, images.Count, (int j) =>
                {
                    var image2 = images[j];
                    var r2 = image2.AbsAspRatio;
                    if (r2 > r1 * aspThr)
                    {
                        progress?.Invoke();
                        return;
                    }

                    var mse = image1.Thumb.GetSimpleMinMse(image2.Thumb, mseThr);
                    if (mse != null)
                    {
                        var task = new SimpleMatchTask(image1, image2, mseThr);
                        task.Run();
                        if (task.Result != null)
                        {
                            result.Add(task.Result);
                        }
                    }

                    progress?.Invoke();
                });
            });
            manager.TaskManager.Stop();
            return result;
        }

        public static IEnumerable<ImageProxy> OrderByAbsAspRatio(this IEnumerable<ImageProxy> yimageFileTuples)
        {
            return yimageFileTuples.OrderBy(x => x.AbsAspRatio);
        }

        private static double? TryToFineCompare(ImageProxy image1, ImageProxy image2, double mseThr, int maxAttempts = 16)
        {
            image1.IsEngaged = true;
            if (!image1.IsEngaged) return null;
            image2.IsEngaged = true;
            if (!image2.IsEngaged) return null;
            return image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
        }
    }
}
