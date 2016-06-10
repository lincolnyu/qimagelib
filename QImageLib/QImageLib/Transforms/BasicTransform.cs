namespace QImageLib.Transforms
{
    public class BasicTransform : ITransform
    {
        public enum Types
        {
            None = 0,
            HorizontalFlip,
            VerticalFlip,
            Rotate180,
            RotateCw90,
            RotateCcw90,
            RotateCw90AndVf,
            RotateCcw90AndVf
        }
        public BasicTransform(Types type)
        {
            Type = type;
        }
        public Types Type { get; }
    }
}
