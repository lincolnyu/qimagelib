using ImageCompLibWin.Data;
using System;
using System.Text;

namespace ImageCompLibWin.SimpleMatch
{
    public static class MatchResults
    {
        public abstract class MatchResult
        {
            protected MatchResult(ImageProxy image1, ImageProxy image2)
            {
                Image1 = image1;
                Image2 = image2;
            }
            public ImageProxy Image1 { get; }
            public ImageProxy Image2 { get; }
        }

        public class ImagesMatch : MatchResult
        {
            public ImagesMatch(ImageProxy image1, ImageProxy image2, double mse) : base(image1, image2)
            {
                Mse = mse;
            }
            public double Mse { get; }
        }

        public class MatchError : MatchResult
        {
            public enum Errors
            {
                YImageError
            }

            public const string YImageNull = "Null Y images";

            public MatchError(ImageProxy image1, ImageProxy image2, Errors error, Exception e, string message) : base(image1, image2)
            {
                Error = error;
                Exception = e;
                Message = message;
            }

            public MatchError(ImageProxy image1, ImageProxy image2, Errors error, Exception e) : this (image1, image2, error, e, null)
            {
            }

            public MatchError(ImageProxy image1, ImageProxy image2, Errors error, string message) : this (image1, image2, error, null, message)
            {
            }

            public Errors Error { get; }

            public Exception Exception { get; }

            public string Message { get; }

            public string GetErrorDescription()
            {
                var sb = new StringBuilder();
                switch (Error)
                {
                    case Errors.YImageError:
                        sb.Append("YImageError");
                        break;
                    default:
                        sb.Append("UnknownError");
                        break;
                }
                if (Exception != null)
                {
                    sb.Append($", (ex: {Exception.GetType().Name}, exmsg: {Exception.Message})");
                }
                if (Message != null)
                {
                    sb.Append($", msg: {Message}");
                }
                return sb.ToString();
            }
        }

        public static int CompareMatchResults(MatchResult r1, MatchResult r2)
        {
            var m1 = r1 as ImagesMatch;
            var m2 = r2 as ImagesMatch;
            if (m1 == null && m2 == null) return 0;
            if (m1 == null) return 1;
            if (m2 == null) return -1;
            return m1.Mse.CompareTo(m2.Mse);
        }
    }
}
