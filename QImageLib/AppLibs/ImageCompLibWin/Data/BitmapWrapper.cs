using System;
using System.Drawing;

namespace ImageCompLibWin.Data
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
            Content = bitmap;
            CacheAwaitingTemp = cache;
        }

        ~BitmapWrapper()
        {
            Dispose(false);
        }

        public Bitmap Content { get; private set; }

        public ImageCache CacheAwaitingTemp { get; }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool dispose)
        {
            if (Content != null)
            {
                CacheAwaitingTemp?.ReleaseTempImage();
                if (dispose)
                {
                    Content.Dispose();
                    Content = null;
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}
