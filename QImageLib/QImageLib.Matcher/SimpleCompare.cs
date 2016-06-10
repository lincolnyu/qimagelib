using QImageLib.Images;
using System;

namespace QImageLib.Matcher
{
    public class SimpleCompare
    {
        public double Compare(IYImage image1, IYImage image2)
        {
            var r1 = (double)image1.NumCols * image2.NumRows / (image1.NumRows * image2.NumCols);
            // W1/H1 < W2/H2
            var r2 = (double)image1.NumCols * image2.NumCols / (image1.NumRows * image2.NumRows);

            if (r1 < 1) r1 = 1 / r1;
            if (r2 < 1) r2 = 1 / r2;

            const double t = 0.2;
            var image2x = new YImageAdapter(image2);
            var minSAD = double.MaxValue;
            if (Math.Abs(r1 - 1) < t)
            {
                // row to row
                var swap = image1.NumRows > image2x.NumRows;
                var sadorig = GetMAD(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.HorizontalFlip;
                var sadh = GetMAD(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.VerticalFlip;
                var sadv = GetMAD(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.Rotate180;
                var sadt = GetMAD(image1, image2x, swap);
                minSAD = Math.Min(minSAD, sadorig);
                minSAD = Math.Min(minSAD, sadh);
                minSAD = Math.Min(minSAD, sadv);
                minSAD = Math.Min(minSAD, sadt);
            }
            if (Math.Abs(r2 - 1) < t)
            {
                // row to col
                var swap = image1.NumRows > image2x.NumCols;
                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCw90;
                var sadcw = GetMAD(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCcw90;
                var sadccw = GetMAD(image1, image2x, swap);

                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCw90AndVf;
                var sadcwv = GetMAD(image1, image2x, swap);
                image2x.BasicTransform = Transforms.BasicTransform.Types.RotateCcw90AndVf;
                var sadccwv = GetMAD(image1, image2x, swap);
                minSAD = Math.Min(minSAD, sadcw);
                minSAD = Math.Min(minSAD, sadccw);
                minSAD = Math.Min(minSAD, sadcwv);
                minSAD = Math.Min(minSAD, sadccwv);
            }
            return minSAD;
        }

        public double GetMAD(IYImage image1, IYImage image2)
        {
            var swap = image1.NumCols > image2.NumCols;
            return GetMAD(image1, image2, swap);
        }

        public double GetMAD(IYImage image1, IYImage image2, bool swap)
        {
            return swap ? GetMADH1NgH2(image2, image1) : GetMADH1NgH2(image1, image2);
        }

        public double GetMADH1NgH2(IYImage image1, IYImage image2)
        {
            var h1 = image1.NumRows;
            var w1 = image1.NumCols;
            var h2 = image2.NumRows;
            var w2 = image2.NumCols;
            var sumdd = 0;
            //h1<=h2
            if (w1 <= w2)
            {
                for (var i = 0; i < h1; i++)
                {
                    var i2 = i * h2 / h1;
                    var i22 = (i + 1) * h2 / h1;
                    for (var j = 0; j < w1; j++)
                    {
                        var j2 = j * w2 / w1;
                        var j22 = (j + 1) * w2 / w1;

                        var s = 0;
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

                        var s1 = 0;
                        for (var jj = j2; jj < j22; jj++)
                        {
                            s1 += image1[i, jj];
                        }
                        s1 /= j22 - j2;

                        var s2 = 0;
                        for (var ii = i2; ii < i22; ii++)
                        {
                            s2 += image2[j, ii];
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
