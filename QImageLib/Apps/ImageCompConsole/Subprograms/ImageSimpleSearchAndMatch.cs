using ImageCompConsole.Global;
using ImageCompLibWin.Data;
using ImageCompLibWin.Helpers;
using ImageCompLibWin.SimpleMatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageCompConsole.Subprograms
{
    class ImageSimpleSearchAndMatch : Subprogram
    {
        public static ImageSimpleSearchAndMatch Instance { get; } = new ImageSimpleSearchAndMatch();

        public override string Subcommand { get; } = "sm";

        public override void PrintUsage(string appname, int indent, int contentIndent)
        {
            var indentStr = new string(' ', indent);
            var contentIndentStr = new string(' ', indent + contentIndent);
            Console.WriteLine(indentStr + "To search for similar images in the direcory and its subdirectories (-q to turn on quite mode)");
            Console.WriteLine(contentIndentStr + LeadingCommandString(appname) + " {<base directory>|[-l] <list file>} [-q] [-o <report file>]");
        }

        public override void Run(string[] args)
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

        private static bool TestY(ImageProxy image)
        {
            if (image.HasFastHisto)
            {
                return image.IsValidY;
            }
            else
            {
                return image.YImage != null;
            }
        }

        private static void PreprocessVerboseParallel(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles)
        {
            var validCount = 0;
            var totalCount = 0;
            var localImageList = new List<ImageProxy>();
            var localInvalidFiles = new List<FileInfo>();
            Parallel.ForEach(imageEnum, (image) =>
            {
                Interlocked.Increment(ref totalCount);
                if (TestY(image))
                {
                    localImageList.Add(image);
                    lock (localImageList)
                    {
                        validCount++;
                        if (ConsoleHelper.CanFreqPrint())
                        {
                            $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InPlaceWriteToConsole();
                            ConsoleHelper.UpdateLastPrintTime();
                        }
                        $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InPlaceWriteToConsole();
                    }
                }
                else
                {
                    lock (localInvalidFiles)
                    {
                        localInvalidFiles.Add(image.File);
                        if (ConsoleHelper.CanFreqPrint())
                        {
                            $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InPlaceWriteToConsole();
                            ConsoleHelper.UpdateLastPrintTime();
                        }
                        $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InPlaceWriteToConsole();
                    }
                }
            });
            imageList = localImageList;
            invalidFiles = localInvalidFiles;
        }

        private static void PreprocessSequential(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles, bool verbose)
        {
            var validCount = 0;
            var totalCount = 0;
            imageList = new List<ImageProxy>();
            invalidFiles = new List<FileInfo>();
            foreach (var image in imageEnum)
            {
                totalCount++;
                if (TestY(image))
                {
                    imageList.Add(image);
                    validCount++;
                    if (verbose)
                    {
                        if (ConsoleHelper.CanFreqPrint())
                        {
                            $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InPlaceWriteToConsole();
                            ConsoleHelper.UpdateLastPrintTime();
                        }
                        $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InPlaceWriteToConsole();
                    }
                }
                else
                {
                    invalidFiles.Add(image.File);
                    if (verbose)
                    {
                        if (ConsoleHelper.CanFreqPrint())
                        {
                            $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InPlaceWriteToConsole();
                            ConsoleHelper.UpdateLastPrintTime();
                        }
                        $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InPlaceWriteToConsole();
                    }
                }
            }
        }

        private static void PreprocessVerbosePostSortAndPrint(bool exportReport, List<ImageProxy> imageList, IList<FileInfo> invalidFiles)
        {
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
            imageList.Sort((a, b) => a.AbsAspRatio.CompareTo(b.AbsAspRatio));
            Console.WriteLine("Files sorted...");
        }

        private static void PreprocessSilentParallel(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles)
        {
            var localImageList = new List<ImageProxy>();
            var localInvalidFiles = new List<FileInfo>();
            Parallel.ForEach(imageEnum, (image) =>
            {
                if (TestY(image))
                {
                    lock (localImageList)
                    {
                        localImageList.Add(image);
                    }
                }
                else
                {
                    lock (localInvalidFiles)
                    {
                        localInvalidFiles.Add(image.File);
                    }
                }
            });
            localImageList.Sort((a, b) => a.AbsAspRatio.CompareTo(b.AbsAspRatio));
            imageList = localImageList;
            invalidFiles = localInvalidFiles;
        }

        private static void ShowMatches(string reportFile, IEnumerable<SimpleImageMatch> matches, ICollection<FileInfo> invalidFiles, bool verbose)
        {
            if (reportFile != null)
            {
                if (verbose) Console.WriteLine("Exporting matched images...");
                using (var reportWriter = new StreamWriter(reportFile))
                {
                    foreach (var match in matches)
                    {
                        var mrep = $"{match.Image1.Path}|{match.Image2.Path}:{match.Mse}";
                        reportWriter.WriteLine(mrep);
                    }
                    if (invalidFiles.Count > 0)
                    {
                        reportWriter.WriteLine(">>>>>>>>>> Ignored Files >>>>>>>>>>");
                        foreach (var invalid in invalidFiles)
                        {
                            reportWriter.WriteLine(invalid.FullName);
                        }
                    }
                }
                if (verbose) Console.WriteLine($"List of images is exported to '{reportFile}'");
            }
            else
            {
                if (verbose) Console.WriteLine("Printing matched images...");
                foreach (var match in matches)
                {
                    var mrep = $"{match.Image1.Path}|{match.Image2.Path}:{match.Mse}";
                    Console.WriteLine(mrep);
                }
                if (verbose) Console.WriteLine("End of matching images");
            }
        }

        private static IEnumerable<SimpleImageMatch> MatchImages(IList<ImageProxy> imageList, bool parallel, bool verbose)
        {
            IEnumerable<SimpleImageMatch> matches;
            if (verbose)
            {
                Console.WriteLine("Matching image files" + (parallel ? " in parallel" : "") + " ...");

                Common.ResetInPlaceWriting();

                var total = ((long)imageList.Count - 1) * imageList.Count / 2;
                int tasks = 0;
                Common.StartProgress();
                if (parallel)
                {
                    matches = imageList.SimpleSearchAndMatchImagesParallel(() =>
                    {
                        lock (imageList)
                        {
                            tasks++;
                            Common.PrintProgress(tasks, total);
                        }
                    }).OrderBy(x => x.Mse).ToList();
                }
                else
                {
                    matches = imageList.SimpleSearchAndMatchImages((i, j) =>
                    {
                        tasks++;
                        Common.PrintProgress(tasks, total);
                    }).OrderBy(x => x.Mse).ToList();
                }

                Common.PrintProgress(total, total, true); // print the 100%

                Console.WriteLine();
                Console.WriteLine("Image matching completed.");
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
            return matches;
        }

        private static void SearchAndMatch(IEnumerable<ImageProxy> imageEnum, string reportFile, bool verbose = false, bool parallel = false)
        {
            try
            {
                if (verbose)
                {
                    Console.WriteLine("Collecting image files...");
                }
                reportFile = !string.IsNullOrWhiteSpace(reportFile) ? reportFile : null;
                List<ImageProxy> imageList;
                List<FileInfo> invalidFiles;
                if (verbose)
                {
                    Common.ResetInPlaceWriting();
                    if (parallel)
                    {
                        PreprocessVerboseParallel(imageEnum, out imageList, out invalidFiles);
                    }
                    else
                    {
                        PreprocessSequential(imageEnum, out imageList, out invalidFiles, true);
                    }
                    PreprocessVerbosePostSortAndPrint(reportFile != null, imageList, invalidFiles);
                }
                else
                {
                    if (parallel)
                    {
                        PreprocessSilentParallel(imageEnum, out imageList, out invalidFiles);
                    }
                    else
                    {
                        PreprocessSequential(imageEnum, out imageList, out invalidFiles, false);
                    }
                }
                var matches = MatchImages(imageList, parallel, verbose);
                ShowMatches(reportFile, matches, invalidFiles, verbose);
            }
            catch (Exception e)
            {
                Common.PrintException(e, verbose);
            }
        }
    }
}
