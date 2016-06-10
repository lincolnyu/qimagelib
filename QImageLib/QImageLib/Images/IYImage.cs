namespace QImageLib.Images
{
    public interface IYImage
    {
        byte this[int y, int x] { get; }
        int NumRows { get; }
        int NumCols { get; }
    }
}
