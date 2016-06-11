using QImageLib.Images;
using System;

namespace QImageLib.Matcher
{
    public static class SimpleCompare
    {
        /// <summary>
        ///  Returns the min MSE comparing two images with all possible simple transformations
        /// </summary>
        /// <param name="image1">The first image</param>
        /// <param name="image2">The second image</param>
        /// <returns>The minimum MSE</returns>
        public static double GetSimpleMinMse(this IYImage image1, IYImage image2)
        {
            var r1 = (double)image1.NumCols * image2.NumRows / (image1.NumRows * image2.NumCols);
            // W1/H1 < W2/H2
            var r2 = (double)image1.NumCols * image2.NumCols / (image1.NumRows * image2.NumRows);

            if (r1 < 1) r1 = 1 / r1;
            if (r2 < 1) r2 = 1 / r2;

            const double t = 0.2;
            var image2x = new YImageAdapter(image2);
            var minMse = double.MaxValue;
            if (Math.Abs(r1 - 1) < t)
            {
                // row to row
                var swap = image1.NumRows > image2x.NumRows;
                var mseOrig = GetMse(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.HorizontalFlip;
                var mseh = GetMse(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.VerticalFlip;
                var msev = GetMse(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.Rotate180;
                var mset = GetMse(image1, image2x, swap);
                minMse = Math.Min(minMse, mseOrig);
                minMse = Math.Min(minMse, mseh);
                minMse = Math.Min(minMse, msev);
                minMse = Math.Min(minMse, mset);
            }
            if (Math.Abs(r2 - 1) < t)
            {
                // row to col
                var swap = image1.NumRows > image2x.NumCols;
                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCw90;
                var msecw = GetMse(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCcw90;
                var mswccw = GetMse(image1, image2x, swap);

                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCw90AndVf;
                var msecwv = GetMse(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCcw90AndVf;
                var mseccwv = GetMse(image1, image2x, swap);
                minMse = Math.Min(minMse, msecw);
                minMse = Math.Min(minMse, mswccw);
                minMse = Math.Min(minMse, msecwv);
                minMse = Math.Min(minMse, mseccwv);
            }
            return minMse;
        }

        public static double GetMse(this IYImage image1, IYImage image2)
        {
            var swap = image1.NumCols > image2.NumCols;
            return GetMse(image1, image2, swap);
        }

        public static double GetMse(IYImage image1, IYImage image2, bool swap)
        {
            return swap ? GetMseH1NgH2(image2, image1) : GetMseH1NgH2(image1, image2);
        }

        public static double GetMseH1NgH2(IYImage image1, IYImage image2)
        {
            var h1 = image1.NumRows;
            var w1 = image1.NumCols;
            var h2 = image2.NumRows;
            var w2 = image2.NumCols;
            var sumdd = 0.0;
            // assuming h1<=h2
            if (h1 == h2 && w1 == w2)
            {
                for (var i = 0; i < h1; i++)
                {
                    for (var j = 0; j < w1; j++)
                    {
                        var d = image1[i, j] - image2[i, j];
                        var dd = d * d;
                        sumdd += dd;
                    }
                }
                return sumdd / (h1 * w1);
            }
            else if (w1 <= w2)
            {
                for (var i = 0; i < h1; i++)
                {
                    var i2 = i * h2 / h1;
                    var i22 = (i + 1) * h2 / h1;
                    for (var j = 0; j < w1; j++)
                    {
                        var j2 = j * w2 / w1;
                        var j22 = (j + 1) * w2 / w1;

                        var s = 0.0;
                        for (var ii = i2; ii < i22; ii++)
                        {
                            for (var jj = j2; jj < j22; jj++)
                            {
                                s += image2[ii,jj];
                            }
                        }
                        s /= (i22 - i2) * (j22 - j2);
                        var d = s - image1[i, j];
                        var dd = d * d;
                        sumdd += dd;
                    }
                }
                return sumdd / (h1 * w1);
            }
            else //  w1 > w2
            {
                for (var i = 0; i < h1; i++)
                {
                    var i2 = i * h2 / h1;
                    var i22 = (i + 1) * h2 / h1;
                    for (var j = 0; j < w2; j++)
                    {
                        var j2 = j * w1 / w2;
                        var j22 = (j + 1) * w1 / w2;

                        var s1 = 0.0;
                        for (var jj = j2; jj < j22; jj++)
                        {
                            s1 += image1[i, jj];
                        }
                        s1 /= j22 - j2;

                        var s2 = 0.0;
                        for (var ii = i2; ii < i22; ii++)
                        {
                            s2 += image2[ii, j];
                        }
                        s2 /= i22 - i2;

                        var d = s1 - s2;
                        var dd = d * d;
                        sumdd += dd;
                    }
                }
                return sumdd / (h1 * w2);
            }
        }
    }
}
