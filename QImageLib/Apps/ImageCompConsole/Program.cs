using System.Drawing;
using ImageCompLibWin;
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using QImageLib.Matcher;
using QImageLib.Images;
using System.Collections.Generic;
using QImageLib.Helpers;

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
                    SearchAndMatch(dir, !args.Contains("-q"));
                }            
            }
            catch (Exception)
            {
                Console.WriteLine("Something went wrong. Please check the input.");
                PrintUsage();
            }
        }

        private static void SearchAndMatch(string dir, bool verbose=false)
        {
            if(verbose)
            {
                Console.WriteLine("Collecting image files...");
            }
            IList<Tuple<IYImage, FileInfo>> imageList;
            if (verbose)
            {
                var localList = new List<Tuple<IYImage, FileInfo>>();
                imageList = localList;
                var imageEnum = dir.GetYImageFileTuples();
                foreach (var image in imageEnum)
                {
                    Console.WriteLine($"{image.Item2.FullName} collected.");
                    imageList.Add(image);
                }
                localList.Sort((a, b) => a.Item1.GetAbsAspectRatio().CompareTo(b.Item1.GetAbsAspectRatio()));
            }
            else
            {
                imageList = dir.GetYImageFileTuples().OrderByAbsAspRatio().ToList();
            }
            if (verbose)
            {
                Console.WriteLine("Image files collected.");
                Console.WriteLine("Matching image files...");
            }
            var matches = imageList.SimpleSearchAndMatchImages().OrderBy(x => x.Mse);
            foreach (var match in matches)
            {
                Console.WriteLine($"{match.Path1},{match.Path2},{match.Mse}");
            }
            if (verbose)
            {
                Console.WriteLine("Image matching completed.");
            }
        }

        private static void Comp(string path1, string path2)
        {
            var bmp1 = (Bitmap)Image.FromFile(path1);
            var bmp2 = (Bitmap)Image.FromFile(path2);
            var mse = bmp1.GetSimpleMinMse(bmp2);
            var similar = mse <= ImageComp.DefaultMseThr;
            var ssim = similar ? "similar" : "different";
            Console.WriteLine($"Simple min MSE = {mse}, images considered {ssim}.");
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
