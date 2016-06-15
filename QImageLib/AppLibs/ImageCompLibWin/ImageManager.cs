namespace ImageCompLibWin
{
    public class ImageManager
    {
        public const int DefaultHistoSize = 8;
        public const int DefaultHistoTotal = 256;

        public ImageManager(ImageCache cache, int histoSize = DefaultHistoSize,
            int histoTotal = DefaultHistoTotal)
        {
            Cache = cache;
            HistoSize = histoSize;
            HistoTotal = histoTotal;
        }

        public static ImageManager Instance { get; } = new ImageManager(ImageCache.Instance);

        public ImageCache Cache { get;  }

        public int HistoTotal { get; }

        public int HistoSize { get; }

        public bool SuppressFastHisto { get; set; }
    }
}
