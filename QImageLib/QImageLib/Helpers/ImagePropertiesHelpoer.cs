using QImageLib.Images;

namespace QImageLib.Helpers
{
    public static class ImagePropertiesHelpoer
    {
        public static double GetAspectRatio(this IYImage image)
        {
            return (double)image.NumCols / image.NumRows;
        }

        /// <summary>
        ///  Returns the aspect ratio or its reciprocal which ever is no less than 1
        /// </summary>
        /// <returns>The absolute aspect ratio</returns>
        public static double GetAbsAspectRatio(this IYImage image)
        {
            return image.NumCols < image.NumRows ?
                (double)image.NumRows / image.NumCols :
                (double)image.NumCols / image.NumRows;
        }
    }
}
