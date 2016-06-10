using QImageLib.Transforms;

namespace QImageLib.Images
{
    public class YImageAdapter : IYImage
    {
        public YImageAdapter(IYImage image)
        {
            OriginalImage = image;
        }

        public IYImage OriginalImage { get; }

        public byte this[int y, int x]
        {
            get
            {
                switch (BasicTransform)
                {
                    case Transforms.BasicTransform.Types.None:
                        return OriginalImage[y, x];
                    case Transforms.BasicTransform.Types.HorizontalFlip:
                        return OriginalImage[y, OriginalImage.NumCols - x - 1];
                    case Transforms.BasicTransform.Types.VerticalFlip:
                        return OriginalImage[OriginalImage.NumRows - y - 1, x];
                    case Transforms.BasicTransform.Types.Rotate180:
                        return OriginalImage[OriginalImage.NumRows - y - 1, OriginalImage.NumCols- x - 1];
                    case Transforms.BasicTransform.Types.RotateCw90:
                        return OriginalImage[OriginalImage.NumCols - x - 1, y];
                    case Transforms.BasicTransform.Types.RotateCcw90:
                        return OriginalImage[x, OriginalImage.NumRows - y - 1];
                    case Transforms.BasicTransform.Types.RotateCw90AndVf:
                        return OriginalImage[OriginalImage.NumCols - x - 1, OriginalImage.NumRows - y - 1];
                    case Transforms.BasicTransform.Types.RotateCcw90AndVf:
                        return OriginalImage[x, y];
                    default:
                        throw new System.Exception("Unexpected transformation");
                }
            }
        }
        public int NumRows => ((int)BasicTransform < 4)?
            OriginalImage.NumRows : OriginalImage.NumCols;
        public int NumCols => ((int)BasicTransform < 4) ?
            OriginalImage.NumCols : OriginalImage.NumRows;
        public BasicTransform.Types BasicTransform { get;
            set;
        }
    }
}
