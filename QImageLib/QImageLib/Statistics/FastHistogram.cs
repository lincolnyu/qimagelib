using QImageLib.Images;

namespace QImageLib.Statistics
{
    public static class FastHistogram
    {
        public static void ClearFastHisto(this int[] histo)
        {
            var size = histo.Length;
            for (var i = 0; i < size; i++)
            {
                histo[i] = 0;
            }
        }

        public static void GenerateFastHistoFromYImage(this int[] histo, IYImage image, int? roughTotal = null)
        {
            var size = histo.Length;
            for (var i = 0; i < image.NumRows; i++)
            {
                for (var j = 0; j < image.NumCols; j++)
                {
                    var y = image[i, j];
                    var b = y * size / 256;
                    histo[b]++;
                }
            }
            if (roughTotal != null)
            {
                var imageSize = image.NumRows * image.NumCols;
                var q = imageSize / roughTotal.Value;
                if (q == 0) q = 1;
                for (var i = 0; i < size; i++)
                {
                    var h = histo[i];
                    h /= q;
                    histo[i] = h;
                }
            }
        }

        public static int Diff(this int[] histo1, int[] histo2)
        {
            var sumd = 0;
            for (var i = 0; i < histo1.Length; i++)
            {
                var d = histo1[i] - histo2[i];
                sumd += d < 0 ? -d : d;
            }
            return sumd;
        }
    }
}
