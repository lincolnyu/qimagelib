namespace ImageCompLibWin
{
    public class SimpleImageMatch
    {
        public SimpleImageMatch(string path1, string path2, double mse)
        {
            Path1 = path1;
            Path2 = path2;
            Mse = mse;
        }
        public string Path1 { get; }
        public string Path2 { get; }
        public double Mse { get; }
    }
}
