using System.Collections.Generic;
using System.Linq;
using QImageLib.Matcher;
using QImageLib.Statistics;
using System.Threading.Tasks;

namespace ImageCompLibWin
{
    public static class ImageSearchAndMatch
    {
        public const double DefaultAspRatioThr = 1.05;
        public const int DefaultHistoThr = 6;

        public delegate void ProgressReport(int i, int j);

        public delegate void ParallelProgressReport();

        public static IEnumerable<SimpleImageMatch> SimpleSearchAndMatchImages(this IList<ImageProxy> images, ProgressReport progress = null,
            double aspThr = DefaultAspRatioThr, double histoThr = DefaultHistoThr,
            double mseThr = ImageComp.DefaultMseThr)
        {
            for (var i = 0; i < images.Count - 1; i++)
            {
                var image1 = images[i];
                var histo1 = image1.FastHisto;
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
                    var histo2 = image2.FastHisto;
                    if (histo1.Diff(histo2) > histoThr)
                    {
                        progress?.Invoke(i, j);
                        continue;
                    }
                    var mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                    if (mse != null)
                    {
                        yield return new SimpleImageMatch(image1, image2, mse.Value);
                    }
                    progress?.Invoke(i, j);
                }
                image1.Release();
            }
        }

        public static IList<SimpleImageMatch> SimpleSearchAndMatchImagesParallel(this IList<ImageProxy> images, ParallelProgressReport progress = null,
          double aspThr = DefaultAspRatioThr, double histoThr = DefaultHistoThr,
          double mseThr = ImageComp.DefaultMseThr)
        {
            var result = new List<SimpleImageMatch>();
            Parallel.For(0, images.Count - 1, (int i) =>
            {
                var image1 = images[i];
                var histo1 = image1.FastHisto;
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
                    var histo2 = image2.FastHisto;
                    if (histo1.Diff(histo2) > histoThr)
                    {
                        progress?.Invoke();
                        return;
                    }
                    var mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                    if (mse != null)
                    {
                        lock(result)
                        {
                            result.Add(new SimpleImageMatch(image1, image2, mse.Value));
                        }
                    }
                    progress?.Invoke();
                });
            });
            return result;
        }

        public static IEnumerable<ImageProxy> OrderByAbsAspRatio(this IEnumerable<ImageProxy> yimageFileTuples)
        {
            return yimageFileTuples.OrderBy(x => x.AbsAspRatio);
        }
    }
}
