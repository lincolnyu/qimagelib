using QImageLib.Images;

namespace QImageLib.Helpers
{
    public static class ImagePropertiesHelper
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

        public static double GetAspectRatio(int width, int height)
        {
            return (double)width / height;
        }

        /// <summary>
        ///  Returns the aspect ratio or its reciprocal which ever is no less than 1
        /// </summary>
        /// <returns>The absolute aspect ratio</returns>
        public static double GetAbsAspectRatio(int width, int height)
        {
            return width < height ? (double)height / width : (double)width / height;
        }
    }
}
