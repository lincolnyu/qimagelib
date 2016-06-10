namespace QImageLib.Transforms
{
    public class Matrix2D : ITransform
    {
        public double A11 { get; set; }
        public double A12 { get; set; }
        public double A21 { get; set; }
        public double A22 { get; set; }
        public double B1 { get; set; }
        public double B2 { get; set; }
    }
}
