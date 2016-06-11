namespace QImageLib.Images
{
    /// <summary>
    ///  Gray scale image
    /// </summary>
    public class YImage : IYImage
    {
        public YImage(int numrows, int numcols)
        {
            Y = new byte[numrows, numcols];
        }
        public byte[,] Y
        {
            get;
        }

        public byte this[int y, int x] => Y[y,x];
        public int NumRows => Y.GetLength(0);
        public int NumCols => Y.GetLength(1);
    }
}
