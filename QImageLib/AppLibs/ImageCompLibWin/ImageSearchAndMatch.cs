using ImageCompLibWin.Helpers;
using QImageLib.Helpers;
using QImageLib.Images;
using QImageLib.Matcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageCompLibWin
{
    public static class ImageSearchAndMatch
    {
        public const double DefaultAspRatioThr = 1.2;

        public static IEnumerable<SimpleImageMatch> SimpleSearchAndMatchImages(this string sdir, int crunch = int.MaxValue, double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr)
        {
            var tuples = sdir.GetYImageFileTuples(crunch).OrderByAbsAspRatio();
            var list = tuples.ToList();
            return list.SimpleSearchAndMatchImages(aspThr, mseThr);
        }

        public static IEnumerable<SimpleImageMatch> SimpleSearchAndMatchImages(this IList<Tuple<IYImage, FileInfo>> yimages, double aspThr = DefaultAspRatioThr, double mseThr = ImageComp.DefaultMseThr)
        {
            for (var i = 0; i < yimages.Count; i++)
            {
                var image1 = yimages[i].Item1;
                var r1 = image1.GetAbsAspectRatio();
                for (var j = i + 1; j < yimages.Count; j++)
                {
                    var image2 = yimages[j].Item1;
                    var r2 = image2.GetAbsAspectRatio();
                    if (r2 > r1 * aspThr)
                    {
                        break;
                    }
                    var mse = image1.GetSimpleMinMse(image2);
                    var similar = mse <= ImageComp.DefaultMseThr;
                    yield return new SimpleImageMatch(yimages[i].Item2.FullName,
                        yimages[j].Item2.FullName, mse);
                }
            }
        }

        public static IEnumerable<Tuple<IYImage, FileInfo>> GetYImageFileTuples(this string sdir, int crunch = int.MaxValue, double aspThr = 1.2, double mseThr = ImageComp.DefaultMseThr)
        {
            var dir = new DirectoryInfo(sdir);
            var tuples = dir.GetYImageFileTuple(crunch);
            return tuples; 
        }

        public static IEnumerable<Tuple<IYImage, FileInfo>> OrderByAbsAspRatio(this IEnumerable<Tuple<IYImage, FileInfo>> yimageFileTuples)
        {
            return yimageFileTuples.OrderBy(x => x.Item1.GetAbsAspectRatio());
        }
    }
}
