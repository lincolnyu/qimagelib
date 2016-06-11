using System.Drawing;
using System.Drawing.Imaging;

namespace ImageCompLibWin.Helpers
{
    public static class BitmapHelper
    {
        public static int GetBitmapWidth(this Bitmap bitmap)
        {
            lock (bitmap)
            {
                return bitmap.Width;
            }
        }

        public static int GetBitmapHeight(this Bitmap bitmap)
        {
            lock (bitmap)
            {
                return bitmap.Height;
            }
        }

        public static PixelFormat GetPixelFormat(this Bitmap bitmap)
        {
            lock (bitmap)
            {
                return bitmap.PixelFormat;
            }
        }
    }
}
