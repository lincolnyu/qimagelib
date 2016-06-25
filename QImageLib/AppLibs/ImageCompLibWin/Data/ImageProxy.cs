using System.IO;
using QImageLib.Images;
using System.Drawing;
using System;
using ImageCompLibWin.Helpers;
using QImageLib.Helpers;

namespace ImageCompLibWin.Data
{
    public class ImageProxy : IResource
    {
        public ImageProxy(FileInfo file, ImageManager imageManager = null)
        {
            File = file;
            ImageManager = imageManager;
        }

        public ImageManager ImageManager { get; }

        /// <summary>
        ///  The image file
        /// </summary>
        public FileInfo File { get; }

        public YImage Thumb { get; private set; }
        public YImage YImage { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Bpp { get; private set; }
        public double AbsAspRatio { get; private set; }
        public string Path => File.FullName;

        public int MaxYConvAttempts => ImageManager?.MaxYConvAttempts ?? ImageManager.DefaultMaxYConvAttempts;

        public int ThumbSize => ImageManager?.ThumbSize ?? ImageManager.DefaultThumbSize;

        #region IResource members

        public int HoldCount
        {
            get; set;
        }

        public int ReferenceCount
        {
            get; set;
        }

        public bool IsEngaged
        {
            get
            {
                return YImage != null;
            }

            set
            {
                if (!value)
                {
                    Release();
                }
                else if (YImage == null)
                {
                    LoadYImage(MaxYConvAttempts);
                }
            }
        }

        public int Size { get; private set; }

        #endregion

        public bool TryLoadImageInfo()
        {
            try
            {
                using (var bmp = (Bitmap)Image.FromFile(File.FullName))
                {
                    Width = bmp.Width;
                    Height = bmp.Height;
                    Size = Width * Height;
                    var pfmt = bmp.GetPixelFormat();
                    Bpp = Image.GetPixelFormatSize(pfmt);
                    AbsAspRatio = ImagePropertiesHelper.GetAbsAspectRatio(Width, Height);
                    Thumb = bmp.GetYImage(ThumbSize);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Bitmap TryGetBitmap(ref int numAttempts)
        {
            for (; numAttempts > 0; numAttempts--)
            {
                try
                {
                    return (Bitmap)Image.FromFile(File.FullName);
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        private void LoadYImage(int numAttempts)
        {
            var bmp = TryGetBitmap(ref numAttempts);
            if (bmp != null)
            {
                for (; YImage == null && numAttempts > 0; numAttempts--)
                {
                    try
                    {
                        YImage = bmp.GetYImage();
                        break;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void Release()
        {
            YImage = null;
        }
    }
}
