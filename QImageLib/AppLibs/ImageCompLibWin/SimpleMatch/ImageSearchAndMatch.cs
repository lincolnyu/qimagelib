using System.Collections.Generic;
using System.Linq;
using QImageLib.Matcher;
using System.Threading.Tasks;
using ImageCompLibWin.Data;
using static ImageCompLibWin.SimpleMatch.MatchResults;
using QImageLib.Images;
using System;

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

        public static MatchError CheckYImages(ImageProxy image1, ImageProxy image2, out YImage y1, out YImage y2)
        {
            try
            {
                y1 = image1.YImage;
                y2 = image2.YImage;
                if (y1 == null || y2 == null)
                {
                    return new MatchError(image1, image2, MatchError.Errors.YImageError, MatchError.YImageNull);
                }
            }
            catch (Exception e)
            {
                y1 = null;
                y2 = null;
                return new MatchError(image1, image2, MatchError.Errors.YImageError, e);
            }
            return null;
        }

        public static IEnumerable<MatchResult> SimpleSearchAndMatchImages(this IList<ImageProxy> images, ProgressReport progress = null,
            double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr)
        {
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

                    var mse = image1.Thumb.GetSimpleMinMse(image2.Thumb, mseThr);
                    if (mse != null)
                    {
                        YImage y1, y2;
                        var error = CheckYImages(image1, image2, out y1, out y2);
                        if (error != null)
                        {
                            yield return error;
                        }

                        mse = y1.GetSimpleMinMse(y2, mseThr);
                    }
                    if (mse != null)
                    {
                        yield return new ImagesMatch(image1, image2, mse.Value);
                    }

                    progress?.Invoke(i, j);
                }
                image1.IsEngaged = false;
            }
        }

        public static IEnumerable<MatchResult> SimpleSearchAndMatchImagesHasty(this IEnumerable<ImageProxy> images, ProgressReport progress = null,
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

                    var mse = image1.Thumb.GetSimpleMinMse(image2.Thumb, mseThr);
                    if (mse != null)
                    {
                        YImage y1, y2;
                        var error = CheckYImages(image1, image2, out y1, out y2);
                        if (error != null)
                        {
                            yield return error;
                        }

                        mse = y1.GetSimpleMinMse(y2, mseThr);
                    }
                    if (mse != null)
                    {
                        hasty1.Excluded = true;
                        hasty2.Excluded = true;
                        yield return new ImagesMatch(image1, image2, mse.Value);
                        image2.IsEngaged = false;
                    }
                    progress?.Invoke(i, j);
                }
                image1.IsEngaged = false;
            }
        }

        public static IList<MatchResult> SimpleSearchAndMatchImagesParallel(this IList<ImageProxy> images, ParallelProgressReport progress = null,
          double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr)
        {
            var result = new List<MatchResult>();
            var manager = images.FirstOrDefault()?.ImageManager;
            if (manager == null) return result;
            manager.TaskManager.Start();

            var tiSeq = SimpleMatchTaskInfo.GenerateTaskSequence(images.Count);
            Parallel.ForEach(tiSeq, ti =>
            {
                var image1 = images[ti.Index1];
                var image2 = images[ti.Index2];
                var r1 = image1.AbsAspRatio;
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

            manager.TaskManager.Stop();
            return result;
        }

        public static IEnumerable<ImageProxy> OrderByAbsAspRatio(this IEnumerable<ImageProxy> yimageFileTuples)
        {
            return yimageFileTuples.OrderBy(x => x.AbsAspRatio);
        }
    }
}
