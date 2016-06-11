using QImageLib.Conversions;
using QImageLib.Images;
using System;
using System.Drawing;

namespace ImageCompLibWin.Helpers
{
    public static class ImageCastHelper
    {
        public static YImage GetYImage(this Bitmap bmp, int crunch = int.MaxValue)
        {
            if (bmp.Height > crunch || bmp.Width > crunch)
            {
                var r = bmp.Height > bmp.Width ? (double)bmp.Height / crunch : (double)bmp.Width / crunch;
                var w = (int)Math.Round(bmp.Width / r);
                var h = (int)Math.Round(bmp.Height / r);
                var yimage = new YImage(h, w);
                for (var i = 0; i < h; i++)
                {
                    var ii = (int)(i * r + 0.5);
                    for (var j = 0; j < w; j++)
                    {
                        var jj = (int)(j * r + 0.5);
                        var pixel = bmp.GetPixel(jj, ii);
                        byte y;
                        RgbToYCbCr.RgbToY8bitBt601(pixel.R, pixel.G, pixel.B, out y);
                        yimage.Y[i, j] = y;
                    }
                }
                return yimage;
            }
            else
            {
                var yimage = new YImage(bmp.Height, bmp.Width);
                for (var i = 0; i < bmp.Height; i++)
                {
                    for (var j = 0; j < bmp.Width; j++)
                    {
                        var pixel = bmp.GetPixel(j, i);
                        byte y;
                        RgbToYCbCr.RgbToY8bitBt601(pixel.R, pixel.G, pixel.B, out y);
                        yimage.Y[i, j] = y;
                    }
                }
                return yimage;
            }
        }
    }
}
