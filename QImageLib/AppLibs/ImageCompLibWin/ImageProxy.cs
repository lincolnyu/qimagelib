using System;
using System.IO;
using System.Drawing;
using ImageCompLibWin.Helpers;
using QImageLib.Images;
using QImageLib.Helpers;
using QImageLib.Statistics;

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

        public int HistoSize => Manager?.HistoSize ?? ImageManager.DefaultHistoSize;

        public int HistoTotal => Manager?.HistoTotal ?? ImageManager.DefaultHistoTotal;

        /// <summary>
        ///  The image file
        /// </summary>
        public FileInfo File { get; }

        public States State { get; private set; }

        public YConfFlags YConvFlag { get; private set; }

        public bool RetainBitmap { get; set; }

        /// <summary>
        ///  return the backing field _bitmap or get a temporary one
        ///  This is for the external user to call
        /// </summary>
        public BitmapWrapper Bitmap
        {
            get
            {
                switch (State)
                {
                    case States.Init:
                        return InitGetBitmap();
                    case States.ImageValid:
                        return ImageValidGetBitmap();
                }
                return null;
            }
        }

        public YImage YImage
        {
            get
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
        }

        public int[] FastHisto
        {
            get
            {
                if (_fastHisto == null)
                {
                    var dummy = YImage;
                }
                return _fastHisto;
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
                InitLoadBitmapInfo();
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
                    bmp = GetBitmap();
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
                else if (RetainBitmap)
                {
                    return LoadBitmapAndItsInfo();
                }
                else
                {
                    return GetBitmap();
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
            if (RetainBitmap)
            {
                return TryConvY(bmp);
            }
            else
            {
                using (bmp)
                {
                    return TryConvY(bmp);
                }
            }
        }

        private YImage TryConvY(BitmapWrapper bmp)
        {
            try
            {
                var ysize = GetYImageSize();
                Cache?.Request(this, ysize);
                _yimage = bmp.Bitmap.GetYImage();
                LoadFastHisto();
                YConvFlag = YConfFlags.Succeeded;
                return _yimage;
            }
            catch (Exception)
            {
                ReleaseYImage();
                _fastHisto = null;
                YConvFlag = YConfFlags.Failed;
                // state remains unchanged
                return _yimage;
            }
        }

        public void Release()
        {
            lock(this)
            {
                var totalSize = 0;
                if (_bitmap != null)
                {
                    totalSize += GetBitmapSize();
                    _bitmap.Dispose();
                    _bitmap = null;
                }
                if (_yimage != null)
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

        public void ReleaseYImage()
        {
            lock(this)
            {
                var totalSize = 0;
                if (_yimage != null)
                {
                    totalSize += GetYImageSize();
                    _yimage = null;
                }
                if (Cache == null) return;
                if (_bitmap != null && _yimage != null)
                {
                    Cache.Release(this, totalSize);
                }
                else if (totalSize > 0)
                {
                    Cache.ReleasePartial(totalSize);
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
                if (_bitmap != null && _yimage != null)
                {
                    Cache.Release(this, totalSize);
                }
                else if (totalSize > 0)
                {
                    Cache.ReleasePartial(totalSize);
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

        private BitmapWrapper GetBitmap()
        {
            return new BitmapWrapper((Bitmap)Image.FromFile(File.FullName), RetainBitmap ? null : Cache);
        }

        public void ReleaseTempBitmap()
        {
            Cache?.ReleaseTempImage();
        }
        
        private BitmapWrapper GetBitmapAndLoadItsInfo()
        {
            var bmp = GetBitmap();
            _width = bmp.Bitmap.GetBitmapWidth();
            _height = bmp.Bitmap.GetBitmapHeight();
            var pfmt = bmp.Bitmap.GetPixelFormat();
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
            using (GetBitmapAndLoadItsInfo())
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
            var bmp = GetBitmapAndLoadItsInfo();
            var bmpSize = GetBitmapSize();
            Cache?.Request(this, bmpSize);
            _bitmap = bmp;
            return bmp;
        }

        private void LoadFastHisto()
        {
            _fastHisto = new int[HistoSize];
            _fastHisto.ClearFastHisto();
            _fastHisto.GenerateFastHistoFromYImage(_yimage, HistoTotal);
        }
    }
}
