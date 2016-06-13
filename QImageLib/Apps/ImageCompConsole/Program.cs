using System.Drawing;
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using QImageLib.Matcher;
using ImageCompLibWin;
using ImageCompLibWin.Helpers;
using System.Threading.Tasks;
using System.Threading;

namespace ImageCompConsole
{
    class Program
    {
        const int LineLen = 118;

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
                else if (args[0] == "-sm")
                {
                    var dir = args[1];
                    var report = args.GetSwitchValue("-o");
                    var verbose = !args.Contains("-q");
                    var parallel = args.Contains("-p");
                    var listfile = args.GetSwitchValue("-l");
                    if (listfile != null)
                    {
                        SearchAndMatchInList(listfile, report, verbose, parallel);
                    }
                    else
                    {
                        SearchAndMatchInDir(dir, report, verbose, parallel);
                    }
                }            
                else if (args[0] == "-s")
                {
                    var dir = args[1];
                    var report = args.GetSwitchValue("-o");
                    var verbose = !args.Contains("-q");
                    var check = args.Contains("-c");
                    Search(dir, report, check, verbose);
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Invalid command arguments.");
                PrintUsage();
            }
        }

        private static void Search(string sdir, string report, bool check, bool verbose)
        {
            var dir = new DirectoryInfo(sdir);
            var imageEnum = dir.GetImageFiles();
            if (check)
            {
                imageEnum = imageEnum.Where(x => new ImageProxy(x).IsValidY);
            }
            var imageFiles = imageEnum.ToList();
            if (verbose)
            {
                Console.WriteLine("Collecting image files...");
            }
            var exportReport = !string.IsNullOrWhiteSpace(report);
            if (verbose && exportReport)
            {
                ConsoleHelper.ResetInPlaceWriting(LineLen);
                var count = 0;
                string lastFile = null;
                foreach (var imageFile in imageFiles)
                {
                    count++;
                    if (ConsoleHelper.CanFreqPrint())
                    {
                        $"{count} file(s) found, last being {imageFile.Name}".InPlaceWriteToConsole();
                    }
                    lastFile = imageFile.Name;
                }
                $"{count} file(s) found, last being {lastFile}".InPlaceWriteToConsole();
                Console.WriteLine();
            }
            else
            {
                foreach (var imageFile in imageFiles)
                {
                    Console.WriteLine(imageFile.FullName);
                }
            }
            if (verbose)
            {
                Console.WriteLine("Image collection completed.");
            }
          
            if (exportReport)
            {
                using (var sw = new StreamWriter(report))
                {
                    foreach (var imageFile in imageFiles)
                    {
                        sw.WriteLine(imageFile.FullName);
                    }
                }
                if (verbose)
                {
                    Console.WriteLine($"List of image files is exported to '{report}'");
                }
            }
        }

        private static void SearchAndMatchInList(string listfile, string report, bool verbose, bool parallel)
        {
            var imageEnum = GetImagesFromListFile(listfile).GetImages(ImageManager.Instance);
            SearchAndMatch(imageEnum, report, verbose, parallel);
        }

        private static IEnumerable<string> GetImagesFromListFile(string listfile)
        {
            using (var sr = new StreamReader(listfile))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (line == null) break;
                    yield return line;
                }
            }
        }

        private static void SearchAndMatchInDir(string sdir, string report, bool verbose = false, bool parallel = false)
        {
            var dir = new DirectoryInfo(sdir);
            var imageEnum = dir.GetImages(ImageManager.Instance);
            SearchAndMatch(imageEnum, report, verbose, parallel);
        }

        private static void SearchAndMatch(IEnumerable<ImageProxy> imageEnum, string report, bool verbose=false, bool parallel=false)
        {
            try
            {
                if (verbose)
                {
                    Console.WriteLine("Collecting image files...");
                }
                var exportReport = !string.IsNullOrWhiteSpace(report);
                IList<ImageProxy> imageList;
                var invalidFiles = new List<FileInfo>();
                if (verbose)
                {
                    var localList = new List<ImageProxy>();
                    var validCount = 0;
                    var totalCount = 0;
                    ConsoleHelper.ResetInPlaceWriting(LineLen);
                    if (parallel)
                    {
                        Parallel.ForEach(imageEnum, (image) =>
                        {
                            Interlocked.Increment(ref totalCount);
                            if (image.IsValidY)
                            {
                                localList.Add(image);
                                lock(localList)
                                {
                                    validCount++;
                                    if (ConsoleHelper.CanFreqPrint())
                                    {
                                        $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InPlaceWriteToConsole();
                                        ConsoleHelper.UpdateLastPrintTime();
                                    }
                                }
                            }
                            else
                            {
                                lock(invalidFiles)
                                {
                                    invalidFiles.Add(image.File);
                                    if (ConsoleHelper.CanFreqPrint())
                                    {
                                        $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InPlaceWriteToConsole();
                                        ConsoleHelper.UpdateLastPrintTime();
                                    }
                                }
                            }
                        });
                    }
                    else
                    {
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
                    }
                   
                    Console.WriteLine();
                    if (!exportReport && invalidFiles.Count > 0)
                    {
                        Console.WriteLine("Following files ignored");
                        foreach (var invalid in invalidFiles)
                        {
                            Console.WriteLine(invalid.FullName);
                        }
                    }
                    Console.WriteLine("Sorting files ...");
                    localList.Sort((a, b) => a.AbsAspRatio.CompareTo(b.AbsAspRatio));
                    Console.WriteLine("Files sorted...");
                    imageList = localList;
                }
                else
                {
                    if (parallel)
                    {
                        var localList = new List<ImageProxy>();
                        Parallel.ForEach(imageEnum, (image) =>
                        {
                            if (image.IsValidY)
                            {
                                lock(localList)
                                {
                                    localList.Add(image);
                                }
                            }
                            else
                            {
                                lock (invalidFiles)
                                {
                                    invalidFiles.Add(image.File);
                                }
                            }
                        });
                        localList.Sort((a, b) => a.AbsAspRatio.CompareTo(b.AbsAspRatio));
                        imageList = localList;
                    }
                    else
                    {
                        imageList = imageEnum.Where(x => x.IsValidY).OrderByAbsAspRatio().ToList();
                    }
                }
                IEnumerable<SimpleImageMatch> matches;
                if (verbose)
                {
                    Console.WriteLine("Matching image files" + (parallel ? " in parallel" : "") + " ...");
                    ConsoleHelper.ResetInPlaceWriting(LineLen);

                    var total = ((long)imageList.Count - 1) * imageList.Count / 2;
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
                        if (invalidFiles.Count > 0)
                        {
                            reportWriter.WriteLine("Files failed to process and ignored:");
                            foreach (var invalid in invalidFiles)
                            {
                                reportWriter.WriteLine(invalid.FullName);
                            }
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

        private static void PrintProgress(int tasks, long total)
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
            Console.WriteLine("  To find out all images files in the directory and its subdirectory (-c to check image)");
            Console.WriteLine("\t" + appname + " -s <base directory> [-c] [-o <report file>]");
            Console.WriteLine("  To search for similar images in the direcory and its subdirectories (-q to turn on quite mode)");
            Console.WriteLine("\t" + appname + " -sm {<base directory>|[-l] <list file>} [-q] [-o <report file>]");
            Console.WriteLine("  To show this usage help: ");
            Console.WriteLine("\t" + appname + " --help");
        }
    }
}
