using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageCompLibWin.Data;
using System;

namespace ImageCompLibWin.Helpers
{
    public static class ImageEnumeration
    {
        public class ErrorItem
        {
            public enum ErrorTypes
            {
                ErrorLoadingImageFile,
                ErrorLoadingSubfolder,
                ErrorLoadingImage,
            }

            public ErrorItem (ErrorTypes error, Exception exception)
            {
                Error = error;
                Exception = exception;
            }

            public ErrorTypes Error { get; }
            public Exception Exception { get; }
        }

        public class Error
        {
            public Error(string location)
            {
                Location = location;
            }
            public string Location { get; }
            public IList<ErrorItem> Errors { get; } = new List<ErrorItem>();
        }

        public class ImageEnumErrors
        {
            public IDictionary<string, Error> Errors { get; } = new Dictionary<string, Error>();

            public void AddError(string location, ErrorItem.ErrorTypes errorType, Exception exception)
            {
                Error err;
                if (!Errors.TryGetValue(location, out err))
                {
                    err = new Error(location);
                }
                err.Errors.Add(new ErrorItem(errorType, exception));
            }
        }

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

        public static IEnumerable<ImageProxy> GetImages(this IEnumerable<string> files, ImageManager imageManager = null)
        {
            return files.Select(x => new FileInfo(x)).Select(x => new ImageProxy(x, imageManager));
        }

        public static IEnumerable<FileInfo> GetImageFiles(this DirectoryInfo dir, ImageEnumErrors errors)
        {
            IEnumerable<FileInfo> imagesThisFolder = null;
            try
            {
                var jpg = dir.GetFiles("*.jpg");
                var jpeg = dir.GetFiles("*.jpeg");
                var gif = dir.GetFiles("*.gif");
                var png = dir.GetFiles("*.png");
                var bmp = dir.GetFiles("*.bmp");
                imagesThisFolder = jpg.Concat(jpeg).Concat(gif).Concat(png).Concat(bmp);
            }
            catch (Exception e)
            {
                errors.AddError(dir.FullName, ErrorItem.ErrorTypes.ErrorLoadingImageFile, e);
                yield break;
            }
            foreach (var image in imagesThisFolder)
            {
                yield return image;
            }
            IEnumerable<DirectoryInfo> subfolders = null;
            try
            {
                subfolders = dir.GetDirectories();
            }
            catch (Exception e)
            {
                errors.AddError(dir.FullName, ErrorItem.ErrorTypes.ErrorLoadingSubfolder, e);
                yield break;
            }
            foreach (var subfolder in subfolders)
            {
                var sfimages = subfolder.GetImageFiles(errors);
                foreach (var sfimage in sfimages)
                {
                    yield return sfimage;
                }
            }
        }

        public static IEnumerable<ImageProxy> GetImages(this DirectoryInfo dir, ImageEnumErrors errors, ImageManager imageManager = null)
        {
            var imageFiles = dir.GetImageFiles(errors);
            foreach (var imageFile in imageFiles)
            {
                ImageProxy ip;
                try
                {
                    ip = new ImageProxy(imageFile, imageManager);
                }
                catch (Exception e)
                {
                    ip = null;
                    errors.AddError(imageFile.FullName, ErrorItem.ErrorTypes.ErrorLoadingImage, e);
                }
                if (ip != null)
                {
                    yield return ip;
                }
            }
        }

        public static IEnumerable<ImageProxy> GetImages(this IEnumerable<string> paths, ImageEnumErrors errors, ImageManager imageManager = null)
        {
            foreach (var path in paths)
            {
                ImageProxy ip;
                try
                {
                    var imageFile = new FileInfo(path);
                    ip = new ImageProxy(imageFile, imageManager);
                }
                catch (Exception e)
                {
                    ip = null;
                    errors.AddError(path, ErrorItem.ErrorTypes.ErrorLoadingImage, e);
                }
                if (ip != null)
                {
                    yield return ip;
                }
            }
        }
    }
}
