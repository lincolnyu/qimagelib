using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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
        
        public static void GetBuffer(this Bitmap bitmap, int bmpWidth, int bmpHeight, PixelFormat pfmt, out byte[] buf,
            out int stride)
        {
            lock (bitmap)
            {
                var bdata = bitmap.LockBits(new Rectangle(0, 0, bmpWidth, bmpHeight), ImageLockMode.ReadOnly, pfmt);
                stride = bdata.Stride;
                var size = stride * bdata.Height;
                buf = new byte[size];
                Marshal.Copy(bdata.Scan0, buf, 0, size);
                bitmap.UnlockBits(bdata);
            }
        }
    }
}
