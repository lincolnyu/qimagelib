using System;
using System.Drawing;

namespace ImageCompLibWin
{
    public class BitmapWrapper : IDisposable
    {
        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="bitmap">The bitmap wrapped</param>
        /// <param name="cache">If it's a temp bitmap, it's the cache to release it or it must be null</param>
        public BitmapWrapper(Bitmap bitmap, ImageCache cache)
        {
            cache?.RequestTempImage();
            Bitmap = bitmap;
            CacheAwaitingTemp = cache;
        }

        public Bitmap Bitmap { get; private set; }

        public ImageCache CacheAwaitingTemp { get; }

        public void Dispose()
        {
            if (Bitmap != null)
            {
                CacheAwaitingTemp?.ReleaseTempImage();
                Bitmap.Dispose();
                Bitmap = null;
            }
        }
    }
}
