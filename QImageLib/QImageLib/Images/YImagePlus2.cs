using QImageLib.Transforms;

namespace QImageLib.Images
{
    public class YImagePlus2 : YImage
    {
        public YImagePlus2(int numrows, int numcols) : base(numrows, numcols)
        {
        }

        /// <summary>
        ///  Transformation to be applied on the natural
        ///  image Y represents to form the image this object
        ///  represents
        /// </summary>
        public ITransform Transform { get; set; }
    }
}
