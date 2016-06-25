using ImageCompConsole.Global;
using ImageCompLibWin.Data;
using ImageCompLibWin.Helpers;
using ImageCompLibWin.SimpleMatch;
using QLogger.ConsoleHelpers;
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
        public enum Modes
        {
            Sequential,
            SequentialHasty,
            Parallel
        }

        public const int PreprocessParallelism = 16;

        public ImageSimpleSearchAndMatch(ImageManager imageManager)
        {
            ImageManager = imageManager;
        }

        public ImageSimpleSearchAndMatch() : this (ImageManager.Instance)
        {
        }

        public static ImageSimpleSearchAndMatch Instance { get; } = new ImageSimpleSearchAndMatch();

        public ImageManager ImageManager { get; }

        public override string Subcommand { get; } = "sm";

        public override void PrintUsage(string appname, int indent, int contentIndent)
        {
            var indentStr = new string(' ', indent);
            var contentIndentStr = new string(' ', indent + contentIndent);
            Console.WriteLine(indentStr + "To search for similar images in the direcory and its subdirectories (-q to turn on quite mode, -p to run parallel, -h sequential hasty mode)");
            Console.WriteLine(contentIndentStr + LeadingCommandString(appname) + " {<base directory>|[-l] <list file>} [-p|-h] [-q] [-o <report file>]");
        }

        public override void Run(string[] args)
        {
            var dir = args[1];
            var report = args.GetSwitchValue("-o");
            var verbose = !args.Contains("-q");
            var parallel = args.Contains("-p");
            var listfile = args.GetSwitchValue("-l");
            var hasty = args.Contains("-h");
            var mode = hasty ? Modes.SequentialHasty : parallel ? Modes.Parallel : Modes.Sequential;
            var imageManager = ImageManager.Instance;
            if (listfile != null)
            {
                SearchAndMatchInList(imageManager, listfile, report, verbose, mode);
            }
            else
            {
                SearchAndMatchInDir(imageManager, dir, report, verbose, mode);
            }
        }

        private static void SearchAndMatchInList(ImageManager manager, string listfile, string report, bool verbose, Modes mode)
        {
            var imageEnum = GetImagesFromListFile(listfile).GetImages(manager);
            SearchAndMatch(imageEnum, report, verbose, mode);
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

        private static void SearchAndMatchInDir(ImageManager manager, string sdir, string report, bool verbose = false, Modes mode =  Modes.Sequential)
        {
            var dir = new DirectoryInfo(sdir);
            var imageEnum = dir.GetImages(manager);
            SearchAndMatch(imageEnum, report, verbose, mode);
        }

        private static bool TestImage(ImageProxy image)
        {
            return image.TryLoadImageInfo();
        }

        private static void PreprocessVerboseParallel(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles)
        {
            var validCount = 0;
            var totalCount = 0;
            var localImageList = new List<ImageProxy>();
            var localInvalidFiles = new List<FileInfo>();
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = PreprocessParallelism
            };
            Parallel.ForEach(imageEnum, options, (image) =>
            {
                Interlocked.Increment(ref totalCount);
                if (TestImage(image))
                {
                    System.Diagnostics.Debug.Assert(image != null);
                    localImageList.Add(image);
                    lock (localImageList)
                    {
                        validCount++;
                        $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InplaceWrite();
                    }
                }
                else
                {
                    lock (localInvalidFiles)
                    {
                        localInvalidFiles.Add(image.File);
                        $"{validCount}/{totalCount} file(s) collected. Last ignored: {image.File.Name}.".InplaceWrite();
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
                if (TestImage(image))
                {
                    imageList.Add(image);
                    validCount++;
                    if (verbose)
                    {
                        $"{validCount}/{totalCount} file(s) collected. Last collected: {image.File.Name}.".InplaceWrite();
                    }
                }
                else
                {
                    invalidFiles.Add(image.File);
                    if (verbose)
                    {
                        $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InplaceWrite();
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
            imageList.Sort((a, b) =>
            {
                System.Diagnostics.Debug.Assert(a != null && b != null);
                return a.AbsAspRatio.CompareTo(b.AbsAspRatio);
            });
            Console.WriteLine("Files sorted...");
        }

        private static void PreprocessSilentParallel(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles)
        {
            var localImageList = new List<ImageProxy>();
            var localInvalidFiles = new List<FileInfo>();
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = PreprocessParallelism
            };
            Parallel.ForEach(imageEnum, options, (image) =>
            {
                if (TestImage(image))
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

        private static string GetModeName(Modes mode)
        {
            switch (mode)
            {
                case Modes.Parallel:
                    return "Parallel";
                case Modes.Sequential:
                    return "Sequential";
                case Modes.SequentialHasty:
                    return "Sequential Hasty";
            }
            throw new ArgumentException("Unexpected image search and match mode");
        }

        private static IEnumerable<SimpleImageMatch> MatchImages(IList<ImageProxy> imageList, Modes mode, bool verbose)
        {
            IEnumerable<SimpleImageMatch> matches;
            if (verbose)
            {
                var modeName = GetModeName(mode);
                Console.WriteLine("Matching image files in " + modeName + " mode ...");

                Common.ResetInPlaceWriting();

                var total = ((long)imageList.Count - 1) * imageList.Count / 2;
                int tasks = 0;
                Common.StartProgress();
                switch (mode)
                {
                    case Modes.Parallel:
                        matches = imageList.SimpleSearchAndMatchImagesParallel(() =>
                        {
                            lock (imageList)
                            {
                                tasks++;
                                Common.PrintProgress(tasks, total);
                            }
                        }).OrderBy(x => x.Mse).ToList();
                        break;
                    case Modes.Sequential:
                        matches = imageList.SimpleSearchAndMatchImages((i, j) =>
                        {
                            tasks++;
                            Common.PrintProgress(tasks, total);
                        }).OrderBy(x => x.Mse).ToList();
                        break;
                    case Modes.SequentialHasty:
                        matches = imageList.SimpleSearchAndMatchImagesHasty((i, j) =>
                        {
                            tasks++;
                            Common.PrintProgress(tasks, total);
                        }).OrderBy(x => x.Mse).ToList();
                        break;
                    default:
                        throw new ArgumentException("Unexpected image search and match mode");
                }
                
                Common.PrintProgress(total, total, true); // print the 100%

                Console.WriteLine();
                Console.WriteLine("Image matching completed.");
            }
            else
            {
                switch (mode)
                {
                    case Modes.Parallel:
                        matches = imageList.SimpleSearchAndMatchImagesParallel().OrderBy(x => x.Mse);
                        break;
                    case Modes.Sequential:
                        matches = imageList.SimpleSearchAndMatchImages().OrderBy(x => x.Mse);
                        break;
                    case Modes.SequentialHasty:
                        matches = imageList.SimpleSearchAndMatchImagesHasty().OrderBy(x => x.Mse);
                        break;
                    default:
                        throw new ArgumentException("Unexpected image search and match mode");
                }
            }
            return matches;
        }

        private static void SearchAndMatch(IEnumerable<ImageProxy> imageEnum, string reportFile, bool verbose = false, Modes mode = Modes.Sequential)
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
                    if (mode == Modes.Parallel)
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
                    if (mode == Modes.Parallel)
                    {
                        PreprocessSilentParallel(imageEnum, out imageList, out invalidFiles);
                    }
                    else
                    {
                        PreprocessSequential(imageEnum, out imageList, out invalidFiles, false);
                    }
                }
                var matches = MatchImages(imageList, mode, verbose);
                ShowMatches(reportFile, matches, invalidFiles, verbose);
            }
            catch (Exception e)
            {
                Common.PrintException(e, verbose);
            }
        }
    }
}
