using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using QImageLib.Conversions;
using QImageLib.Images;

namespace ImageCompLibWin.Helpers
{
    public static class ImageCastHelper
    {
        /// <summary>
        ///  Returns true if the bitmap can be successfully converted to YImage
        ///  It must be consistent with the behaviour of GetYImage()
        /// </summary>
        /// <param name="bmp">The bitmap to test</param>
        /// <returns>True if YImage convertible</returns>
        public static bool ValidateY(this Bitmap bmp)
        {
            var bpp = Image.GetPixelFormatSize(bmp.PixelFormat);
            return bpp == 24 || bpp == 32;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="crunch"></param>
        /// <returns></returns>
        /// <remarks>
        ///  References: 
        ///   http://stackoverflow.com/questions/11662354/c-sharp-faster-way-to-compare-pixels-between-two-images-and-only-write-out-the-d
        ///   http://davidthomasbernal.com/blog/2008/03/13/c-image-processing-performance-unsafe-vs-safe-code-part-i/
        ///   http://www.codeproject.com/Tips/240428/Work-with-bitmap-faster-with-Csharp
        /// </remarks>
        public static YImage GetYImage(this Bitmap bmp, int crunch = int.MaxValue)
        {
            var bdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            var size = bdata.Stride * bdata.Height;
            var buf = new byte[size];
            Marshal.Copy(bdata.Scan0, buf, 0, size);

            var bpp = Image.GetPixelFormatSize(bmp.PixelFormat);
            if (bpp != 24 && bpp != 32)
            {
                throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
            }
            var bypp = bpp / 8;

            if (bmp.Height > crunch || bmp.Width > crunch)
            {
                var cr = bmp.Height > bmp.Width ? (double)bmp.Height / crunch : (double)bmp.Width / crunch;
                var w = (int)Math.Round(bmp.Width / cr);
                var h = (int)Math.Round(bmp.Height / cr);
                var yimage = new YImage(h, w);

                for (var i = 0; i < h; i++)
                {
                    var ii = (int)(i * cr + 0.5);
                    var pline = ii*bdata.Stride;
                    // TODO this is sampling, not taking mean values
                    for (var j = 0; j < w; j++)
                    {
                        var jj = (int)(j * cr + 0.5);
                        var p = pline + jj * bypp;
                        var r = buf[p];
                        var g = buf[p+1];
                        var b = buf[p+2];
                        byte y;
                        RgbToYCbCr.RgbToY8bitBt601(r, g, b, out y);
                        yimage.Y[i, j] = y;                        
                    }
                }
                return yimage;
            }
            else
            {
                var pline = 0;
                var yimage = new YImage(bmp.Height, bmp.Width);
                for (var i = 0; i < bmp.Height; i++)
                {
                    var p = pline;
                    for (var j = 0; j < bmp.Width; j++, p += bypp)
                    {
                        var r = buf[p];
                        var g = buf[p + 1];
                        var b = buf[p + 2];
                        byte y;
                        RgbToYCbCr.RgbToY8bitBt601(r, g, b, out y);
                        yimage.Y[i, j] = y;
                    }
                    pline += bdata.Stride;
                }
                return yimage;
            }
        }
    }
}
