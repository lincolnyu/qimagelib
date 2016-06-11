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
        public enum ValidationStatuses
        {
            Init = 0,
            InvalidImage,
            ValidImage,
            InvalidY,
            ValidY,
        }

   
        private Bitmap _bitmap;
        private YImage _yimage;
        private int _width;
        private int _height;
        private double _absAspRatio = -1;
        private int[] _fastHisto;

        public ImageProxy(FileInfo file, ImageManager imageManager = null, Bitmap bitmap = null)
        {
            File = file;
            Manager = imageManager;
            _bitmap = bitmap;
        }

        public ImageManager Manager { get; }

        public int HistoSize => Manager?.HistoSize ?? ImageManager.DefaultHistoSize;

        public int HistoTotal => Manager?.HistoTotal ?? ImageManager.DefaultHistoTotal;

        public ImageCache Cache => Manager?.Cache;

        public FileInfo File { get; }

        public ValidationStatuses ValidationStatus { get; private set; }

        public int[] FastHisto
        {
            get
            {
                if (_fastHisto == null)
                {
                    TryLoadFastHisto();
                }
                return _fastHisto;
            }
        }

        public bool IsValidImage
        {
            get
            {
                if (ValidationStatus == ValidationStatuses.Init)
                {
                    TryLoadBitmap();
                }
                // NOTE invalid Y is counted as valid image too
                return ValidationStatus == ValidationStatuses.ValidImage ||
                    ValidationStatus == ValidationStatuses.InvalidY ||
                    ValidationStatus == ValidationStatuses.ValidY;
            }
        }

        public bool IsValidY
        {
            get
            {
                if (ValidationStatus != ValidationStatuses.ValidY
                    && ValidationStatus != ValidationStatuses.InvalidY)
                {
                    var bmp = Bitmap;
                    if (bmp != null)
                    {
                        ValidationStatus = bmp.ValidateY()? ValidationStatuses.ValidY : ValidationStatuses.InvalidY;
                    }
                }
                return ValidationStatus == ValidationStatuses.ValidY;
            }
        }

        private bool AlreadyInvalid => ValidationStatus == ValidationStatuses.InvalidImage;

        public Bitmap Bitmap
        {
            get
            {
                if (_bitmap == null)
                {
                    TryLoadBitmap();
                }
                return _bitmap;
            }
        }

        public YImage YImage
        {
            get
            {
                if (_yimage == null && !AlreadyInvalid)
                {
                    TryGetYImage();
                }
                return _yimage;
            }
        }

        public string Path => File.FullName;

        public int Width => _width > 0 || AlreadyInvalid ? _width : (_width = Bitmap.Width);
        public int Height => _height > 0 || AlreadyInvalid ? _height : (_height = Bitmap.Height);
        public double AbsAspRatio => _absAspRatio > 0 || AlreadyInvalid ? _absAspRatio : (_absAspRatio = ImagePropertiesHelper.GetAbsAspectRatio(Width, Height));

        private void TryLoadBitmap()
        {
            try
            {
                _bitmap = (Bitmap)Image.FromFile(File.FullName);
                _width = Bitmap.Width;
                _height = Bitmap.Height;
                _absAspRatio = ImagePropertiesHelper.GetAbsAspectRatio(Width, Height);
                if (ValidationStatus != ValidationStatuses.InvalidY && ValidationStatus != ValidationStatuses.ValidY)
                {
                    ValidationStatus = ValidationStatuses.ValidImage;
                }
                Cache?.Push(this);
            }
            catch (Exception)
            {
                if (_bitmap != null)
                {
                    _bitmap.Dispose();
                    _bitmap = null;
                }
                ValidationStatus = ValidationStatuses.InvalidImage;
            }
        }

        private void TryGetYImage()
        {
            try
            {
                _yimage = Bitmap.GetYImage();
                Cache?.Push(this);
            }
            catch (Exception)
            {
                _yimage = null;
                if (ValidationStatus != ValidationStatuses.InvalidImage)
                {
                    ValidationStatus = ValidationStatuses.InvalidY;
                }
            }
        }

        private void TryLoadFastHisto()
        {
            try
            {
                _fastHisto = new int[HistoSize];
                _fastHisto.ClearFastHisto();
                _fastHisto.GenerateFastHistoFromYImage(YImage, HistoTotal);
            }
            catch (Exception)
            {
                _fastHisto = null;
            }
        }

        public void Release()
        {
            ReleaseYImage();
            ReleaseBitmap();
            DeCacheIfCan();
        }

        public void ReleaseYImage()
        {
            _yimage = null;
            DeCacheIfCan();
        }
        public void ReleaseBitmap()
        {
            _bitmap?.Dispose();
            _bitmap = null;
            DeCacheIfCan();
        }

        private void DeCacheIfCan()
        {
            if (Cache != null && _bitmap == null && _yimage == null)
            {
                Cache.Remove(this);
            }
        }
    }
}
