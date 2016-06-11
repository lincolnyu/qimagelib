using System.Drawing;
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using QImageLib.Matcher;
using ImageCompLibWin;
using ImageCompLibWin.Helpers;

namespace ImageCompConsole
{
    class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                if (args.Contains("--help"))
                {
                    PrintUsage();
                    return;
                }
                if (args[0] == "-c")
                {
                    var path1 = args[1];
                    var path2 = args[2];
                    Comp(path1, path2);
                }
                else if (args[0] == "-s")
                {
                    var dir = args[1];
                    var report = args.GetSwitchValue("-o");
                    SearchAndMatch(dir, report, !args.Contains("-q"));
                }            
            }
            catch (Exception)
            {
                Console.WriteLine($"Invalid command arguments.");
                PrintUsage();
            }
        }

        

        private static void SearchAndMatch(string sdir, string report, bool verbose=false)
        {
            const int lineLen = 118;
            try
            {
                if (verbose)
                {
                    Console.WriteLine("Collecting image files...");
                }
                IList<ImageProxy> imageList;
                var dir = new DirectoryInfo(sdir);
                if (verbose)
                {
                    var localList = new List<ImageProxy>();
                    var imageEnum = dir.GetImages(ImageManager.Instance);
                    var validCount = 0;
                    var totalCount = 0;
                    ConsoleHelper.ResetInPlaceWriting(lineLen);
                    foreach (var image in imageEnum)
                    {
                        totalCount++;
                        if (image.IsValidY)
                        {
                            validCount++;
                            $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InPlaceWriteToConsole();
                            localList.Add(image);
                        }
                        else
                        {
                            $"{validCount}/{totalCount} file(s) collected. Last ignored: {image.File.Name}.".InPlaceWriteToConsole();
                        }
                    }
                    Console.WriteLine();
                    Console.WriteLine("Sorting files ...");
                    localList.Sort((a, b) => a.AbsAspRatio.CompareTo(b.AbsAspRatio));
                    Console.WriteLine("Files sorted...");
                    imageList = localList;
                }
                else
                {
                    imageList = dir.GetImages(ImageManager.Instance).Where(x => x.IsValidY).OrderByAbsAspRatio().ToList();
                }
                IEnumerable<SimpleImageMatch> matches;
                if (verbose)
                {
                    Console.WriteLine("Matching image files...");
                    ConsoleHelper.ResetInPlaceWriting(lineLen);

                    var total = (imageList.Count - 1) * imageList.Count / 2;
                    matches = imageList.SimpleSearchAndMatchImages((i, j) =>
                    {
                        var il = imageList.Count - i - 2;
                        var left = (il + 1) * il / 2 + (imageList.Count - j - 1);
                        var comp = total - left;
                        var perc = comp * 100 / total;
                        $"{comp}/{total} ({perc}%) completed".InPlaceWriteToConsole();
                    }).OrderBy(x => x.Mse).ToList();
                    Console.WriteLine();
                    Console.WriteLine("Image matching completed.");
                    Console.WriteLine("Printing matching images...");
                }
                else
                {
                    matches = imageList.SimpleSearchAndMatchImages().OrderBy(x => x.Mse);
                }
                StreamWriter reportWriter = null;
                if (!string.IsNullOrWhiteSpace(report))
                {
                    reportWriter = new StreamWriter(report);
                }
                foreach (var match in matches)
                {
                    var mrep = $"{match.Image1.Path},{match.Image2.Path},{match.Mse}";
                    Console.WriteLine(mrep);
                    if (reportWriter != null)
                    {
                        reportWriter.WriteLine(mrep);
                    }
                }
                if (verbose)
                {
                    Console.WriteLine("End of matching images");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong. Details: {e.Message}");
            }
        }

        private static void Comp(string path1, string path2)
        {
            try
            {
                var bmp1 = (Bitmap)Image.FromFile(path1);
                var bmp2 = (Bitmap)Image.FromFile(path2);
                var mse = bmp1.GetSimpleMinMse(bmp2);
                var similar = mse <= ImageComp.DefaultMseThr;
                var ssim = similar ? "similar" : "different";
                Console.WriteLine($"Simple min MSE = {mse}, images considered {ssim}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong. Details: {e.Message}");
            }
        }

        private static void PrintUsage()
        {
            var appname = Assembly.GetExecutingAssembly().GetName().Name;
            Console.WriteLine("ImageComp Tool for Windows (ver 1.0) -- based on QImageLib");
            Console.WriteLine("  To compare two images: ");
            Console.WriteLine("\t" + appname + " -c <path to first image> <path to second image>");
            Console.WriteLine("  To search for similar images in subdirectories (-q to turn on quite mode)");
            Console.WriteLine("\t" + appname + " -s <base directory> [-q]");
            Console.WriteLine("  To show this usage help: ");
            Console.WriteLine("\t" + appname + " --help");
        }
    }
}
