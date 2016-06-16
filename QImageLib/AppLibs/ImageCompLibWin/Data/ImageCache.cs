using System.Collections.Generic;
using System.Threading;

namespace ImageCompLibWin.Data
{
    public class ImageCache
    {
        public const int DefaultQuota = 1024 * 1024 * 1024; // 1G
        public const int DefaultMaxAllowedTempImages = 16;
        public const int ReleaseEventWaitTimeoutMs = 500;
        public const int QueueEventWaitTimeoutMs = 500;

        private readonly Semaphore _tempImageSemaphore;

        private AutoResetEvent _queueAddEvent = new AutoResetEvent(false);


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

        public Dictionary<ImageProxy, int> QueuedImages { get; } = new Dictionary<ImageProxy, int>();
        
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

            ClaimCache(size);
            
            // non block
            lock (this)
            {
                if (QueuedImages.ContainsKey(image))
                {
                    if (ImageQueue.Last.Value != image)
                    {
                        var p = ImageQueue.Find(image);
                        ImageQueue.Remove(p);
                        ImageQueue.AddLast(image);
                        _queueAddEvent.Set();
                    }
                    QueuedImages[image] += size;
                }
                else
                {
                    ImageQueue.AddLast(image);
                    _queueAddEvent.Set();
                    QueuedImages.Add(image, size);
                }
            }               
        }

        private void ClaimCache(int requested)
        {
            var reduceTo = CacheQuota - requested;
            lock(this)
            {
                if (CachedSize < reduceTo)
                {
                    CachedSize += requested;
                    return;
                }
            }

            while (true)
            {
                if (ImageQueue.Count > 0)
                {
                    var image = ImageQueue.First.Value;
                    // release lock
                    image.Release(); // blocked until allowed by the image
                }
                else
                {
                    _queueAddEvent.WaitOne(QueueEventWaitTimeoutMs);
                }
                lock (this)
                {
                    if (CachedSize < reduceTo)
                    {
                        CachedSize += requested;
                        return;
                    }
                }
            }
        }

        public void ReleasePartial(ImageProxy image, int size)
        {
            lock(this)
            {
                QueuedImages[image] -= size;
                System.Diagnostics.Debug.Assert(QueuedImages[image] >= 0);
                CachedSize -= size;
            }
        }

        public void Release(ImageProxy image, int size)
        {
            // non blocking
            lock (this)
            {
                if (QueuedImages.ContainsKey(image))
                {
                    CachedSize -= size;
                    System.Diagnostics.Debug.Assert(QueuedImages[image] == size);
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
