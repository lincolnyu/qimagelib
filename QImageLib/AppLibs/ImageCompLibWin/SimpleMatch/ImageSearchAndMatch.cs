using System.Collections.Generic;
using System.Linq;
using QImageLib.Matcher;
using QImageLib.Statistics;
using System.Threading.Tasks;
using ImageCompLibWin.Helpers;
using ImageCompLibWin.Data;

namespace ImageCompLibWin.SimpleMatch
{
    public static class ImageSearchAndMatch
    {
        public const double DefaultAspRatioThr = 1.05;
        public const int DefaultHistoThr = 6;

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
            double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr,
            double histoThr = DefaultHistoThr)
        {
            bool? histo = null;
            for (var i = 0; i < images.Count - 1; i++)
            {
                var image1 = images[i];
                var histo1 = image1.FastHisto;
                var r1 = image1.AbsAspRatio;
                if (histo == null)
                {
                    histo = image1.HasFastHisto;
                }
                for (var j = i + 1; j < images.Count; j++)
                {
                    var image2 = images[j];
                    var r2 = image2.AbsAspRatio;
                    if (r2 > r1 * aspThr)
                    {
                        progress?.Invoke(i, j);
                        break;
                    }
                    if (histo == true)
                    {
                        var histo2 = image2.FastHisto;
                        if (histo1.Diff(histo2) > histoThr)
                        {
                            progress?.Invoke(i, j);
                            continue;
                        }
                    }

                    var mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                    if (histo != true && mse != null)
                    {
                        using (var bmp1 = image1.Bitmap)
                        using (var bmp2 = image2.Bitmap)
                        {
                            var y1 = bmp1.Content.GetYImage();
                            var y2 = bmp2.Content.GetYImage();
                            mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                        }
                    }
                    if (mse != null)
                    {
                        yield return new SimpleImageMatch(image1, image2, mse.Value);
                    }

                    progress?.Invoke(i, j);
                }
                image1.Release();
            }
        }

        public static IEnumerable<SimpleImageMatch> SimpleSearchAndMatchImagesHasty(this IEnumerable<ImageProxy> images, ProgressReport progress = null,
            double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr,
            double histoThr = DefaultHistoThr)
        {
            bool? histo = null;
            var hastyList = images.Select(x => new HastyImage(x)).ToList();
            for (var i = 0; i < hastyList.Count - 1; i++)
            {
                var hasty1 = hastyList[i];
                if (hasty1.Excluded) continue;
                var image1 = hasty1.WrappedImage;
                var histo1 = image1.FastHisto;
                var r1 = image1.AbsAspRatio;
                if (histo == null)
                {
                    histo = image1.HasFastHisto;
                }
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
                    if (histo == true)
                    {
                        var histo2 = image2.FastHisto;
                        if (histo1.Diff(histo2) > histoThr)
                        {
                            progress?.Invoke(i, j);
                            continue;
                        }
                    }

                    var mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                    if (histo != true && mse != null)
                    {
                        using (var bmp1 = image1.Bitmap)
                        using (var bmp2 = image2.Bitmap)
                        {
                            var y1 = bmp1.Content.GetYImage();
                            var y2 = bmp2.Content.GetYImage();
                            mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                        }
                    }
                    if (mse != null)
                    {
                        hasty1.Excluded = true;
                        hasty2.Excluded = true;
                        yield return new SimpleImageMatch(image1, image2, mse.Value);
                        image2.Release();
                    }
                    progress?.Invoke(i, j);
                }
                image1.Release();
            }
        }

        public static IList<SimpleImageMatch> SimpleSearchAndMatchImagesParallel(this IList<ImageProxy> images, ParallelProgressReport progress = null,
          double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr,
          double histoThr = DefaultHistoThr)
        {
            var result = new List<SimpleImageMatch>();
            bool? histo = null;
            Parallel.For(0, images.Count - 1, (int i) =>
            {
                var image1 = images[i];
                var histo1 = image1.FastHisto;
                var r1 = image1.AbsAspRatio;

                if (histo == null)
                {
                    histo = image1.HasFastHisto;
                }
                Parallel.For(i + 1, images.Count, (int j) =>
                {
                    var image2 = images[j];
                    var r2 = image2.AbsAspRatio;
                    if (r2 > r1 * aspThr)
                    {
                        progress?.Invoke();
                        return;
                    }
                    if (histo == true)
                    {
                        var histo2 = image2.FastHisto;
                        if (histo1.Diff(histo2) > histoThr)
                        {
                            progress?.Invoke();
                            return;
                        }
                    }
                   
                    var mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                    if (histo != true && mse != null)
                    {
                        using (var bmp1 = image1.TryGetBitmap())
                        using (var bmp2 = image2.TryGetBitmap())
                        {
                            var y1 = bmp1.Content.GetYImage();
                            var y2 = bmp2.Content.GetYImage();
                            if (y1 != null && y2 != null)
                            {
                                mse = image1.YImage.GetSimpleMinMse(image2.YImage, mseThr);
                            }
                            else
                            {
                                mse = null;
                            }
                        }
                    }
                    if (mse != null)
                    {
                        lock (result)
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
