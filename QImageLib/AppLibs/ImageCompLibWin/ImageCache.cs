using System.Collections.Generic;

namespace ImageCompLibWin
{
    public class ImageCache
    {
        public const int DefaultCacheSize = 128*1024*1024;

        public ImageCache() : this(DefaultCacheSize)
        {
        }

        public ImageCache(int cacheSize) : this(cacheSize, cacheSize / 2)
        {
        }

        public ImageCache(int cacheSize, int popToSize)
        {
            CacheSize = cacheSize;
            PopToSize = popToSize;
        }

        public static ImageCache Instance { get; } = new ImageCache();

        public int CacheSize { get; }

        public int PopToSize { get; }

        public int CachedSize { get; private set; }

        public LinkedList<ImageProxy> ImageQueue { get; } = new LinkedList<ImageProxy>();

        public HashSet<ImageProxy> QueuedImages { get; } = new HashSet<ImageProxy>();

        private int ImageSize(ImageProxy image)
        {
            return image.Width * image.Height;
        }

        public void Push(ImageProxy image) 
        {
            lock (this)
            {
                if (QueuedImages.Contains(image))
                {
                    if (ImageQueue.Last.Value != image)
                    {
                        var p = ImageQueue.Find(image);
                        ImageQueue.Remove(p);
                        ImageQueue.AddLast(image);
                    }
                }
                else
                {
                    ImageQueue.AddLast(image);
                    QueuedImages.Add(image);
                    var size = ImageSize(image);
                    CachedSize += size;
                    if (CachedSize > CacheSize)
                    {
                        do
                        {
                            Pop();
                        } while (CachedSize > PopToSize);
                    }
                }
            }
        }

        private void Pop()
        {
            if (QueuedImages.Count > 0)
            {
                var image = ImageQueue.First.Value;
                var size = ImageSize(image);
                CachedSize -= size;
                ImageQueue.RemoveFirst();
                QueuedImages.Remove(image);
                image.Release();
            }
        }

        public void Remove(ImageProxy image)
        {
            lock(this)
            {
                if (QueuedImages.Contains(image))
                {
                    ImageQueue.Remove(image);
                    QueuedImages.Remove(image);
                }
            }
        }
    }
}
