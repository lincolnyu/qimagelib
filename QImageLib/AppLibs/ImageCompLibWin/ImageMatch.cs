namespace ImageCompLibWin
{
    public class SimpleImageMatch
    {
        public SimpleImageMatch(ImageProxy image1, ImageProxy image2, double mse)
        {
            Image1 = image1;
            Image2 = image2;
            Mse = mse;
        }
        public ImageProxy Image1 { get; }
        public ImageProxy Image2 { get; }
        public double Mse { get; }
    }
}
