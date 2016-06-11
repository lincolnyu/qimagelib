using System;

namespace QImageLib.Conversions
{
    /// <summary>
    ///  Converts RGB to YCbCr (YUV is a non-well-defined
    ///  equivalent term for analog signals)
    /// </summary>
    /// <references>
    ///  https://en.wikipedia.org/wiki/YCbCr
    ///  http://discoverybiz.net/enu0/faq/faq_YUV_YCbCr_YPbPr.html
    /// </references>
    public static class RgbToYCbCr
    {
        public const double KbBt601 = 0.114;
        public const double KrBt601 = 0.299;

        public const double KbBt709 = 0.0722;

        public const double KrBt709 = 0.2126;

        public const double KbBt2020 = 0.0593;
        public const double KrBt2020 = 0.2627;

        public static void RgbToYPbPr(double r, double g, double b, out double y, double kb, double kr,
            out double pb, out double pr)
        {
            y = kr * r + (1 - kr - kb) * g + kb * b;
            pb = 0.5 * (b - y) / (1 - kb);
            pr = 0.5 * (r - y) / (1 - kr);
        }

        /// <summary>
        ///  analog YPbPr to digital 8 bit YCbCr
        /// </summary>
        /// <param name="y">analog Y (0~1.0)</param>
        /// <param name="pb">analog Pb (0~1.0)</param>
        /// <param name="pr">analog Pr (0~1.0)</param>
        /// <param name="y8">8bit Y</param>
        /// <param name="cb">8bit Cb</param>
        /// <param name="cr">8bit Cr</param>
        public static void YPbPrToYCbCr8bit(double y, double pb, double pr, out double y8, out double cb, out double cr)
        {
            y8 = 16 + 219 * y;
            cb = 128 + 224 * pb;
            cr = 128 + 224 * pr;
        }

        public static void RgbToYCbCr8bitBt601(double r, double g, double b, out double y, out double cb, out double cr)
        {
            y = 16 + (65.738 / 256) * r + (129.057 / 256) * g + (25.064 / 256) * b;
            cb = 128 - (-37.945 / 256) * r - (74.494 / 256) * g + (112.439 / 256) * b;
            cr = 128 + (112.439 / 256) * r - (94.154 / 256) * g - (18.285 / 256) * b;
        }

        public static void YCbCrToRgb8bitBt601(double y, double cb, double cr, out double r, out double g, out double b)
        {
            var oy = y - 16;
            var ou = cb - 128;
            var ov = cr - 128;
            r = (255.0 / 219) * oy + (255.0 / 112 * 0.701) * ov;
            g = (255.0 / 219) * oy + (255.0 / 112 * 0.886 * 0.114 / 0.587) * ou + (255.0 / 112 * 0.701 * 0.299 / 0.587) * ov;
            b = (255.0 / 219) * oy + (255.0 / 112 * 0.886) * ou;
        }

        public static void RgbToY8bitBt601(double r, double g, double b, out double y)
        {
            y = 16 + (65.738 / 256) * r + (129.057 / 256) * g + (25.064 / 256) * b;
        }

        public static void RgbToY8bitBt601(byte r, byte g, byte b, out byte y)
        {
            y = (byte)(16 + (65.738 / 256) * r + (129.057 / 256) * g + (25.064 / 256) * b);
        }
    }
}
