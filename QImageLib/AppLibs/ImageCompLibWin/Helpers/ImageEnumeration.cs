using System.Collections.Generic;
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

        public static IEnumerable<ImageProxy> GetImages(this DirectoryInfo dir, ImageManager imageManager = null)
        {
            return dir.GetImageFiles().Select(x => new ImageProxy(x, imageManager));
        }
    }
}
