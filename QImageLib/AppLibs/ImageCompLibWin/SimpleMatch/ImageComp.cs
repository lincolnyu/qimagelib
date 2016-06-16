using ImageCompLibWin.Helpers;
using QImageLib.Matcher;
using System.Drawing;

namespace ImageCompLibWin.SimpleMatch
{
    public static class ImageComp
    {
        public const double DefaultMseThr = 100;

        public static bool Similar(this Bitmap image1, Bitmap image2, double thrMse = DefaultMseThr)
        {
            var mse = image1.GetSimpleMinMse(image2);
            return mse <= thrMse;
        }

        public static double GetSimpleMinMse(this Bitmap image1, Bitmap image2)
        {
            var yimage1 = image1.GetYImage();
            var yimage2 = image2.GetYImage();
            return yimage1.GetSimpleMinMse(yimage2).Value;
        }
    }
}
