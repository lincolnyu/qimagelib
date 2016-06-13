using System.Collections.Generic;
using System.Threading;

namespace ImageCompLibWin
{
    public class ImageCache
    {
        public const int DefaultQuota = 1024 * 1024 * 1024; // 1G
        public const int DefaultMaxAllowedTempImages = 16;

        private readonly Semaphore _tempImageSemaphore;

        public ImageCache(int quota = DefaultQuota, int maxAllowedTempImages = DefaultMaxAllowedTempImages) : this(quota, quota / 2, maxAllowedTempImages)
        {
        }

        public ImageCache(int quota, int popToSize, int maxAllowedTempImages = DefaultMaxAllowedTempImages)
        {
            CacheQuota = quota;
            PopToSize = popToSize;
            _tempImageSemaphore = new Semaphore(maxAllowedTempImages, maxAllowedTempImages);
        }

        public static ImageCache Instance { get; } = new ImageCache();

        public int CacheQuota { get; }

        public int PopToSize { get; }

        public int CachedSize { get; private set; }

        public LinkedList<ImageProxy> ImageQueue { get; } = new LinkedList<ImageProxy>();

        public HashSet<ImageProxy> QueuedImages { get; } = new HashSet<ImageProxy>();
        
        public void RequestTempImage()
        {
            _tempImageSemaphore.WaitOne();
        }

        public void ReleaseTempImage()
        {
            _tempImageSemaphore.Release();
        }

        public int MaxAllowedImageSize()
        {
            return PopToSize;
        }
        
        public bool SizeAlowed(int size)
        {
            return size <= MaxAllowedImageSize();
        }

        public void Request(ImageProxy image, int size)
        {
            System.Diagnostics.Debug.Assert(size <= MaxAllowedImageSize());

            ReduceCacheIfNeeded(size);

            // non block
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
                    CachedSize += size;
                }
                else
                {
                    ImageQueue.AddLast(image);
                    QueuedImages.Add(image);
                    CachedSize += size;
                }
            }               
        }

        private void ReduceCacheIfNeeded(int requested)
        {
            var reduceTo = CacheQuota - requested;
            if (CachedSize >= reduceTo)
            {
                while (true)
                {
                    var image = ImageQueue.First.Value;
                    // release lock
                    image.Release(); // blocked until allowed by the image
                    lock(this)
                    {
                        if (CachedSize < reduceTo)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public void ReleasePartial(int size)
        {
            lock(this)
            {
                CachedSize -= size;
            }
        }

        public void Release(ImageProxy image, int size)
        {
            // non blocking
            lock (this)
            {
                if (QueuedImages.Contains(image))
                {
                    CachedSize -= size;
                    if (ImageQueue.First.Value == image)
                    {
                        ImageQueue.RemoveFirst();
                    }
                    else
                    {
                        ImageQueue.Remove(image);
                    }
                    QueuedImages.Remove(image);
                }
            }
        }
    }
}
