using System;
using System.Drawing;
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
        /// <param name="bpp">The bpp of the bitmap</param>
        /// <returns>True if YImage convertible</returns>
        public static bool ValidateY(int bpp)
        {
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
            var bmpWidth = bmp.GetBitmapWidth();
            var bmpHeight = bmp.GetBitmapHeight();
            var pfmt = bmp.GetPixelFormat();
            int stride;
            byte[] buf;
            bmp.GetBuffer(bmpWidth, bmpHeight, pfmt, out buf, out stride); 

            var bpp = Image.GetPixelFormatSize(pfmt);
            if (bpp != 24 && bpp != 32)
            {
                throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
            }
            var bypp = bpp / 8;

            if (bmpHeight > crunch || bmpWidth > crunch)
            {
                var cr = bmpHeight > bmpWidth ? (double)bmpHeight / crunch : (double)bmpWidth / crunch;
                var w = (int)Math.Round(bmpWidth / cr);
                var h = (int)Math.Round(bmpHeight / cr);
                var yimage = new YImage(h, w);

                var iistart = 0;
                for (var i = 0; i < h; i++)
                {
                    var iiend = (i + 1) * bmpHeight / h;
                    // TODO this is sampling, not taking mean values
                    var jjstart = 0;
                    for (var j = 0; j < w; j++)
                    {
                        var jjend = (j + 1) * bmpWidth / w;
                        var ysum = 0;
                        for (var ii = iistart; ii < iiend; ii++)
                        {
                            var pline = ii * stride;
                            for (var jj = jjstart; jj < jjend; jj++)
                            {
                                var p = pline + jj * bypp;
                                var r = buf[p];
                                var g = buf[p + 1];
                                var b = buf[p + 2];
                                byte y;
                                RgbToYCbCr.RgbToY8bitBt601(r, g, b, out y);
                                ysum += y;
                            }
                        }
                        ysum /= (iiend - iistart) * (jjend - jjstart);
                        yimage.Y[i, j] = (byte)ysum;
                        jjstart = jjend;
                    }
                    iistart = iiend;
                }
                return yimage;
            }
            else
            {
                var pline = 0;
                var yimage = new YImage(bmpHeight, bmpWidth);
                for (var i = 0; i < bmpHeight; i++)
                {
                    var p = pline;
                    for (var j = 0; j < bmpWidth; j++, p += bypp)
                    {
                        var r = buf[p];
                        var g = buf[p + 1];
                        var b = buf[p + 2];
                        byte y;
                        RgbToYCbCr.RgbToY8bitBt601(r, g, b, out y);
                        yimage.Y[i, j] = y;
                    }
                    pline += stride;
                }
                return yimage;
            }
        }
    }
}
