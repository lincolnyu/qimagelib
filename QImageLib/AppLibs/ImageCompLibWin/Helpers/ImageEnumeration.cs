using QImageLib.Images;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImageCompLibWin.Helpers
{
    public static class ImageEnumeration
    {
        public static IEnumerable<FileInfo> GetImageFiles(this DirectoryInfo dir)
        {
            var jpg = dir.GetFiles("*.jpg", SearchOption.AllDirectories);
            var jpeg = dir.GetFiles("*.jpeg", SearchOption.AllDirectories);
            var gif = dir.GetFiles("*.gif", SearchOption.AllDirectories);
            var png = dir.GetFiles("*.png", SearchOption.AllDirectories);
            var bmp = dir.GetFiles("*.bmp", SearchOption.AllDirectories);
            return jpg.Concat(jpeg).Concat(gif).Concat(png).Concat(bmp);
        }

        public static IEnumerable<Image> GetImages(this DirectoryInfo dir)
        {
            return dir.GetImageFiles().Select(x => Image.FromFile(x.FullName));
        }

        public static IEnumerable<Tuple<Bitmap, FileInfo>> GetImageFileTuple(this DirectoryInfo dir)
        {
            return dir.GetImageFiles().Select(x => new Tuple<Bitmap, FileInfo>((Bitmap)Image.FromFile(x.FullName), x));
        }

        public static IEnumerable<IYImage> GetYImages(this DirectoryInfo dir, int crunch = int.MaxValue)
        {
            return dir.GetImages().Cast<Bitmap>().Select(x => x.GetYImage(crunch));
        }
        
        public static IEnumerable<Tuple<IYImage, FileInfo>> GetYImageFileTuple(this DirectoryInfo dir, int crunch = int.MaxValue)
        {
            return dir.GetImageFileTuple().Select(x =>
             new Tuple<IYImage, FileInfo>(x.Item1.GetYImage(crunch), x.Item2)
            );
        }
    }
}
