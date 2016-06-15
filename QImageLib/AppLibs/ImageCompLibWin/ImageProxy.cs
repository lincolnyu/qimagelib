using System;
using System.IO;
using System.Drawing;
using ImageCompLibWin.Helpers;
using QImageLib.Images;
using QImageLib.Helpers;
using QImageLib.Statistics;
using System.Threading;

namespace ImageCompLibWin
{
    public class ImageProxy
    {
        /// <summary>
        ///   States of the image
        /// </summary>
        /// <remarks>
        ///   State machine
        ///   
        ///          loading bitmap failed                            
        ///          or size rejected by cache                      
        ///    Init ------------------------------) [InvalidImage] 
        ///      \                                                                     
        ///       \         info succ but bmp dropped as no retain              
        ///        \----------------------------------------------------            load bmp failed
        ///         \                                    release bmp     \                 ---  
        ///          \                               ---------------------\              /    |
        ///           |  bmp & info succ          /           bmp succ     v            v     | 
        ///            ------------------) ImageInfoValid+ (----------- ImageInfoValidNoData -
        ///
        /// </remarks>
        public enum States
        {
            Init = 0,               // intial status
            ImageInvalid,           
            ImageValid,             
        }

        public enum YConfFlags
        {
            Init,
            Succeeded,
            Failed
        }

        /// <summary>
        ///  If an image's maximum edge length is no more than below the image's
        ///  Y will not be released
        /// </summary>
        public const int MaxYImageSizeToKeep = 64;

        public delegate void ImageInfoReadyEventHandler(ImageProxy sender);

        private delegate T ReturnBackingField<T>();

        /// <summary>
        ///  unless RetainBitmap is true it's not kept
        /// </summary>
        private BitmapWrapper _bitmap;
        private YImage _yimage;
        private int _width;
        private int _height;
        private int _bpp;
        private double _absAspRatio = -1;
        private int[] _fastHisto;

        public ImageProxy(FileInfo file, ImageManager imageManager = null)
        {
            File = file;
            Manager = imageManager;
        }

        public event ImageInfoReadyEventHandler ImageInfoReady;

        public ImageManager Manager { get; }

        public ImageCache Cache => Manager?.Cache;

        /// <summary>
        ///  The image file
        /// </summary>
        public FileInfo File { get; }

        public States State { get; private set; }

        public YConfFlags YConvFlag { get; private set; }

        public int HistoSize => Manager?.FastHistoSize ?? ImageManager.DefaultFastHistoSize;

        public int HistoSum => Manager?.FastHistoSum ?? ImageManager.DefaultFastHistoSum;

        public bool RetainBitmap => Manager?.SuppressBitmapRetention == false;

        public bool HasFastHisto => Manager?.SuppressFastHisto == false;

        public int CrunchSize => Manager?.CrunchSize ?? int.MaxValue;

        /// <summary>
        ///  return the backing field _bitmap or get a temporary one
        ///  This is for the external user to call
        /// </summary>
        public BitmapWrapper Bitmap
        {
            get
            {
                lock (this)
                {
                    BitmapWrapper result = null;
                    switch (State)
                    {
                        case States.Init:
                            result = InitGetBitmap();
                            break;
                        case States.ImageValid:
                            result = ImageValidGetBitmap();
                            break;
                    }
                    return result;
                }
            }
        }

        public YImage YImage
        {
            get
            {
                lock(this)
                {
                    var result = LoadYImage();
                    return result;
                }
            }
        }

        public int[] FastHisto
        {
            get
            {
                if (!HasFastHisto)
                {
                    return null;
                }
                lock(this)
                {
                    if (_fastHisto == null)
                    {
                        LoadYImage();
                        if (_yimage == null)
                        {
                            return null;
                        }
                        LoadFastHisto();
                    }
                    return _fastHisto;
                }
            }
        }

        public string Path => File.FullName;

        public int Width => GetImageProperty(() => _width);
        public int Height => GetImageProperty(() => _height);
        public int Bpp => GetImageProperty(() => _bpp);

        public int Area => Width * Height;

        /// <summary>
        ///  Aspect ratio
        /// </summary>
        /// <remarks>
        ///  since GetAbsAspectRatio() returns 0 for invalid input, 0 is deemed set value
        /// </remarks>
        public double AbsAspRatio => _absAspRatio >= 0 ? _absAspRatio : (_absAspRatio = ImagePropertiesHelper.GetAbsAspectRatio(Width, Height));

        public bool SizeIsReady => _width > 0 && _height > 0;

        public bool IsValidY
        {
            get
            {
                var bpp = Bpp;
                if (State == States.ImageInvalid) return false;
                return ImageCastHelper.ValidateY(bpp);
            }
        }

        private YImage LoadYImage()
        {
            switch (State)
            {
                case States.Init:
                    return InitGetYImage();
                case States.ImageValid:
                    return ImageValidGetYImage();
            }
            return null;
        }

        private bool YImageToKeep()
        {
            return _width <= MaxYImageSizeToKeep && _height <= MaxYImageSizeToKeep;
        }


        /// <summary>
        ///  Load bitmap and its info for bitmap properties of this instance
        /// </summary>
        /// <typeparam name="T">The type of the property</typeparam>
        /// <param name="returnBackingField">The method that returns the backing field</param>
        /// <returns>The value</returns>
        private T GetImageProperty<T>(ReturnBackingField<T> returnBackingField)
        {
            if (State == States.ImageInvalid) return default(T);
            if (State == States.Init)
            {
                lock (this)
                {
                    InitLoadBitmapInfo();
                }
            }
            return returnBackingField();
        }

        /// <summary>
        ///  Load bitmap information from Init state
        /// </summary>
        private void InitLoadBitmapInfo()
        {
            try
            {
                if (RetainBitmap)
                {
                    LoadBitmapAndItsInfo();
                }
                else
                {
                    LoadBitmapForInfoOnly();
                }
                State = States.ImageValid;
            }
            catch (Exception)
            {
                Release();
                State = States.ImageInvalid;
            }
        }

        /// <summary>
        ///  Get bitmap from Init state
        /// </summary>
        private BitmapWrapper InitGetBitmap()
        {
            try
            {
                BitmapWrapper bmp;
                if (RetainBitmap)
                {
                    bmp = LoadBitmapAndItsInfo();
                }
                else
                {
                    bmp = GetBitmap(false);
                }
                State = States.ImageValid;
                return bmp;
            }
            catch (Exception)
            {
                Release();
                State = States.ImageInvalid;
                return null;
            }
        }

        /// <summary>
        ///  GetYImage from Init state
        /// </summary>
        private YImage InitGetYImage()
        {
            var bmp = InitGetBitmap();
            return GetYImageFromBitmapToReleaseIfNeeded(bmp);
        }

        /// <summary>
        ///  Get bitmap in ImageValid state
        /// </summary>
        private BitmapWrapper ImageValidGetBitmap()
        {
            try
            {
                if (_bitmap != null)
                {
                    return _bitmap;
                }
                if (RetainBitmap)
                {
                    return LoadBitmapAndItsInfo();
                }
                else
                {
                    return GetBitmap(false);
                }
            }
            catch (Exception)
            {
                Release();
                return null; // state remains ImageValid
            }
        }

        /// <summary>
        ///  Get YImage in ImageValid state
        /// </summary>
        private YImage ImageValidGetYImage()
        {
            if (_yimage != null)
            {
                return _yimage;
            }
            var bmp = ImageValidGetBitmap();
            return GetYImageFromBitmapToReleaseIfNeeded(bmp);
        }

        /// <summary>
        ///  Get YImage converted from the bitmap
        /// </summary>
        /// <param name="bmp">The bitmap obtained for YImage purpose only</param>
        /// <returns></returns>
        private YImage GetYImageFromBitmapToReleaseIfNeeded(BitmapWrapper bmp)
        {
            if (bmp == null) return null;
            try
            {
                return TryConvY(bmp);
            }
            finally
            {
                if (!RetainBitmap)
                {
                    bmp.Dispose();
                }
            }
        }

        private YImage TryConvY(BitmapWrapper bmp)
        {
            int ysize = 0;
            try
            {
                ysize = GetYImageSize();
                if (!YImageToKeep())
                {
                    Cache?.Request(this, ysize); // TODO exception from here...
                }
                _yimage = bmp.Content.GetYImage(CrunchSize);
                if (HasFastHisto)
                {
                    LoadFastHisto();
                }
                YConvFlag = YConfFlags.Succeeded;
                return _yimage;
            }
            catch (Exception)
            {
                ReleaseYImageOnError(ysize);
                _fastHisto = null;
                YConvFlag = YConfFlags.Failed;
                // state remains unchanged
                return _yimage;
            }
        }

        public void Release()
        {
            lock (this)
            {
                var totalSize = 0;
                if (_bitmap != null)
                {
                    totalSize += GetBitmapSize();
                    // NOTE we dont dispose the bitmap here
                    // as the user may still need it for a short period hopefully
                    _bitmap.Dispose();
                    _bitmap = null;
                }
                if (_yimage != null && !YImageToKeep())
                {
                    totalSize += GetYImageSize();
                    _yimage = null;
                }
                if (Cache != null)
                {
                    Cache.Release(this, totalSize);
                }
            }
        }

        private void ReleaseYImageOnError(int size)
        {
            _yimage = null;
            if (_bitmap == null)
            {
                Cache.Release(this, size);
            }
            else if (size > 0 && !YImageToKeep())
            {
                Cache.ReleasePartial(this, size);
            }
        }

        public void ReleaseYImage()
        {
            lock (this)
            {
                var totalSize = 0;
                if (_yimage != null)
                {
                    totalSize += GetYImageSize();
                    _yimage = null;
                }
                if (Cache == null) return;
                if (_bitmap == null && _yimage == null)
                {
                    Cache.Release(this, totalSize);
                }
                else if (totalSize > 0)
                {
                    Cache.ReleasePartial(this, totalSize);
                }
            }
        }

        public void ReleaseBitmap()
        {
            lock(this)
            {
                var totalSize = 0;
                if (_bitmap != null)
                {
                    totalSize += GetBitmapSize();
                    _bitmap = null;
                }
                if (Cache == null) return;
                if (_bitmap == null && _yimage == null)
                {
                    Cache.Release(this, totalSize);
                }
                else if (totalSize > 0)
                {
                    Cache.ReleasePartial(this, totalSize);
                }
            }
        }

        /// <summary>
        ///  Must use cached value, assuming image info obtained
        /// </summary>
        /// <returns>The appx byte size of the bitmap</returns>
        private int GetBitmapSize()
        {
            return _width * _height * _bpp;
        }

        /// <summary>
        ///  Must use cached value, assuming image info obtained
        /// </summary>
        /// <returns>The appx byte size of the Y image</returns>
        private int GetYImageSize()
        {
            return _width * _height;
        }

        private BitmapWrapper GetBitmap(bool retain)
        {
            return new BitmapWrapper((Bitmap)Image.FromFile(File.FullName), retain ? null : Cache);
        }

        public void ReleaseTempBitmap()
        {
            Cache?.ReleaseTempImage();
        }
        
        private BitmapWrapper GetBitmapAndLoadItsInfo(bool retain)
        {
            var bmp = GetBitmap(retain);
            _width = bmp.Content.GetBitmapWidth();
            _height = bmp.Content.GetBitmapHeight();
            var pfmt = bmp.Content.GetPixelFormat();
            _bpp = Image.GetPixelFormatSize(pfmt);
            _absAspRatio = ImagePropertiesHelper.GetAbsAspectRatio(_width, _height);
            if (_width == 0 || _height == 0)
            {
                throw new ArgumentException("Image with zero dimensions");
            }
            var size = GetBitmapSize();
            if (Cache?.SizeAlowed(size) == false)
            {
                throw new ArgumentException("Image size too large");
            }
            ImageInfoReady?.Invoke(this);
            return bmp;
        }

        private void LoadBitmapForInfoOnly()
        {
            using (GetBitmapAndLoadItsInfo(false))
            {
            }
        }

        /// <summary>
        ///  Pre: bmp not loaded
        ///  Post: bmp loaded, size requested in cache
        /// </summary>
        /// <returns></returns>
        private BitmapWrapper LoadBitmapAndItsInfo()
        {
            var bmp = GetBitmapAndLoadItsInfo(true);
            var bmpSize = GetBitmapSize();
            Cache?.Request(this, bmpSize); // what if this throws an exception
            _bitmap = bmp;
            return bmp;
        }

        private void LoadFastHisto()
        {
            if (_fastHisto == null) // do only once
            {
                _fastHisto = new int[HistoSize];
                _fastHisto.ClearFastHisto();
                _fastHisto.GenerateFastHistoFromYImage(_yimage, HistoSum);
            }
        }
    }
}
