namespace ImageCompLibWin.Data
{
    public class ImageManager
    {
        public const int DefaultFastHistoSize = 8;
        public const int DefaultFastHistoSum = 256;
        public const int DefaultCrunchSize = 64;

        public ImageManager(ImageCache cache, bool suppressFastHisto = true,
            int crunchSize = DefaultCrunchSize, bool suppressBitmapRetention = true,
            int histoSize = DefaultFastHistoSize, int histoTotal = DefaultFastHistoSum)
        {
            Cache = cache;
            FastHistoSize = histoSize;
            FastHistoSum = histoTotal;
            CrunchSize = DefaultCrunchSize;
            SuppressFastHisto = suppressFastHisto;
            SuppressBitmapRetention = suppressBitmapRetention;
        }

        public static ImageManager Instance { get; } = new ImageManager(ImageCache.Instance);

        public ImageCache Cache { get; }

        public int FastHistoSum { get; }

        public int FastHistoSize { get; }

        public bool SuppressFastHisto { get; }

        public int CrunchSize { get; }

        public bool SuppressBitmapRetention { get; }
    }
}
