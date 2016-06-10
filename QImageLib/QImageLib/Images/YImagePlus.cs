using QImageLib.Transforms;

namespace QImageLib.Images
{
    public class YImagePlus : YImage
    {
        public YImagePlus(int numrows, int numcols) : base(numrows, numcols)
        {
        }

        public new byte this[int y, int x]
        {
            get
            {
                switch (BasicTransform)
                {
                    case Transforms.BasicTransform.Types.None:
                        return base[y, x];
                    case Transforms.BasicTransform.Types.HorizontalFlip:
                        return base[y, base.NumCols - x - 1];
                    case Transforms.BasicTransform.Types.VerticalFlip:
                        return base[base.NumRows - y - 1, x];
                    case Transforms.BasicTransform.Types.Rotate180:
                        return base[base.NumRows - y - 1, base.NumCols - x - 1];
                    case Transforms.BasicTransform.Types.RotateCw90:
                        return base[base.NumCols - x - 1, y];
                    case Transforms.BasicTransform.Types.RotateCcw90:
                        return base[x, base.NumRows - y - 1];
                    case Transforms.BasicTransform.Types.RotateCw90AndVf:
                        return base[base.NumCols - x - 1, base.NumRows - y - 1];
                    case Transforms.BasicTransform.Types.RotateCcw90AndVf:
                        return base[x, y];
                    default:
                        throw new System.Exception("Unexpected transformation");
                }
            }
        }
        public new int NumRows => ((int)BasicTransform < 4) ?
            base.NumRows : base.NumCols;
        public new int NumCols => ((int)BasicTransform < 4) ?
            base.NumCols : base.NumRows;

        public BasicTransform.Types BasicTransform
        {
            get;
            set;
        }
    }
}
