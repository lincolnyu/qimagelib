using System;
using System.Drawing.Imaging;

namespace ImageCompLibWin.Helpers
{
    public static class BitmapHelper
    {
        public static int GetBitsPerPixel(this PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format1bppIndexed:
                    return 1;
                case PixelFormat.Format4bppIndexed:
                    return 4;
                case PixelFormat.Format8bppIndexed:
                    return 8;
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format16bppGrayScale:
                    return 16;
                case PixelFormat.Format24bppRgb:
                    return 24;
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppArgb:
                    return 32;
                case PixelFormat.Format48bppRgb:
                    return 48;
                case PixelFormat.Format64bppPArgb:
                case PixelFormat.Format64bppArgb:
                    return 64;
                default:
                    throw new Exception("Unknown pixel format");
            }
        }
    }
}
