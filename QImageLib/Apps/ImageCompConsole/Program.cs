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
        private static DateTime _startTime;

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
                    var verbose = !args.Contains("-q");
                    var parallel = args.Contains("-p");
                    SearchAndMatch(dir, report, verbose, parallel);
                }            
            }
            catch (Exception)
            {
                Console.WriteLine($"Invalid command arguments.");
                PrintUsage();
            }
        }

        private static void SearchAndMatch(string sdir, string report, bool verbose=false, bool parallel=false)
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
                    var invalidFiles = new List<FileInfo>();
                    foreach (var image in imageEnum)
                    {
                        totalCount++;
                        if (image.IsValidY)
                        {
                            localList.Add(image);
                            validCount++;
                            if (ConsoleHelper.CanFreqPrint())
                            {
                                $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InPlaceWriteToConsole();
                                ConsoleHelper.UpdateLastPrintTime();
                            }
                        }
                        else
                        {
                            invalidFiles.Add(image.File);
                            if (ConsoleHelper.CanFreqPrint())
                            {
                                $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InPlaceWriteToConsole();
                                ConsoleHelper.UpdateLastPrintTime();
                            }
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
                var exportReport = !string.IsNullOrWhiteSpace(report);
                if (verbose)
                {
                    Console.WriteLine("Matching image files" + (parallel ? " in parallel" : "") + " ...");
                    ConsoleHelper.ResetInPlaceWriting(lineLen);

                    var total = (imageList.Count - 1) * imageList.Count / 2;
                    int tasks = 0;
                    StartProgress();
                    if (parallel)
                    {
                        matches = imageList.SimpleSearchAndMatchImagesParallel(() =>
                        {
                            lock(imageList)
                            {
                                tasks++;
                                PrintProgress(tasks, total);
                            }
                        }).OrderBy(x => x.Mse).ToList();
                    }
                    else
                    {
                        matches = imageList.SimpleSearchAndMatchImages((i, j) =>
                        {
                            tasks++;
                            PrintProgress(tasks, total);
                        }).OrderBy(x => x.Mse).ToList();
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("Image matching completed.");
                    if (exportReport)
                    {
                        Console.WriteLine("Exporting matched images...");
                    }
                    else
                    {
                        Console.WriteLine("Printing matched images...");
                    }
                }
                else
                {
                    if (parallel)
                    {
                        matches = imageList.SimpleSearchAndMatchImagesParallel().OrderBy(x => x.Mse);
                    }
                    else
                    {
                        matches = imageList.SimpleSearchAndMatchImages().OrderBy(x => x.Mse);
                    }
                }
                if (exportReport)
                {
                    using (var reportWriter = new StreamWriter(report))
                    {
                        foreach (var match in matches)
                        {
                            var mrep = $"{match.Image1.Path},{match.Image2.Path},{match.Mse}";
                            reportWriter.WriteLine(mrep);
                        }
                    }
                }
                else
                {
                    foreach (var match in matches)
                    {
                        var mrep = $"{match.Image1.Path},{match.Image2.Path},{match.Mse}";
                        Console.WriteLine(mrep);
                    }
                }
                if (verbose)
                {
                    if  (exportReport)
                    {
                        Console.WriteLine($"List of images is exported to '{report}'");
                    }
                    else
                    {
                        Console.WriteLine("End of matching images");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong.");
                if (verbose)
                {
                    var indent = "";
                    do
                    {
                        Console.WriteLine($"{indent}Error type: {e.GetType().Name}");
                        Console.WriteLine($"{indent}Error details: {e.Message}");
                        Console.WriteLine($"{indent}Stack trace: {e.StackTrace}");
                        e = e.InnerException;
                        indent += " "; 
                    } while (e != null);
                }
            }
        }

        private static void StartProgress()
        {
            _startTime = DateTime.UtcNow;
        }

        private static void PrintProgress(int tasks, int total)
        {
            if (ConsoleHelper.CanFreqPrint())
            {
                var curr = DateTime.UtcNow;
                var elapsed = curr - _startTime;
                var estMins = elapsed.TotalMinutes * total / tasks;
                var remain = TimeSpan.FromMinutes(estMins) - elapsed;
                var elapsedstr = elapsed.ToString(@"d\.hh\:mm\:ss");
                var remainstr = remain.ToString(@"d\.hh\:mm");
                
                var perc = tasks * 100 / total;
                $"{tasks}/{total} ({perc}%) completed. {elapsedstr} elapsed, est. {remainstr} remaining.".InPlaceWriteToConsole();
                ConsoleHelper.UpdateLastPrintTime();
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
            Console.WriteLine("\t" + appname + " -s <base directory> [-q] [-o <report file>]");
            Console.WriteLine("  To show this usage help: ");
            Console.WriteLine("\t" + appname + " --help");
        }
    }
}
