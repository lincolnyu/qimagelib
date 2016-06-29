using ImageCompConsole.Global;
using ImageCompLibWin.Data;
using ImageCompLibWin.Helpers;
using ImageCompLibWin.SimpleMatch;
using QLogger.ConsoleHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ImageCompLibWin.SimpleMatch.MatchResults;
using static System.Diagnostics.Debug;

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

        public const int DefaultProprocessParallelism = 16;

        public ImageSimpleSearchAndMatch(ImageManager imageManager)
        {
            ImageManager = imageManager;
        }

        public ImageSimpleSearchAndMatch() : this (ImageManager.Instance)
        {
        }

        public static ImageSimpleSearchAndMatch Instance { get; } = new ImageSimpleSearchAndMatch();

        public ImageManager ImageManager { get; }

        public int PreprocessParallelism { get; private set; } = DefaultProprocessParallelism;

        public override string Subcommand { get; } = "sm";

        public override void PrintUsage(string appname, int indent, int contentIndent)
        {
            var indentStr = new string(' ', indent);
            var contentIndentStr = new string(' ', indent + contentIndent);
            Console.WriteLine(indentStr + "To search for similar images in the direcory and its subdirectories (-q to turn on quite mode, -p to run parallel, -h to use sequential hasty mode, -pp to specify the number of parallel tasks for image file loading and preprocessing (default 16))");
            Console.WriteLine(contentIndentStr + LeadingCommandString(appname) + " {<base directory>|[-l] <list file>} [-p|-h] [-q] [-o <report file>] [-pp <num concurrent tasks>]");
        }

        public override void Run(string[] args)
        {
            var dir = args[1];
            var report = args.GetSwitchValue("-o");
            var verbose = !args.Contains("-q");
            var parallel = args.Contains("-p");
            var listfile = args.GetSwitchValue("-l");
            var hasty = args.Contains("-h");
            var concstr = args.GetSwitchValue("-pp");
            int concurrency;
            if (int.TryParse(concstr, out concurrency))
            {
                PreprocessParallelism = concurrency;
            }
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

        private void SearchAndMatchInList(ImageManager manager, string listfile, string report, bool verbose, Modes mode)
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

        private void SearchAndMatchInDir(ImageManager manager, string sdir, string report, bool verbose = false, Modes mode =  Modes.Sequential)
        {
            var dir = new DirectoryInfo(sdir);
            var imageEnum = dir.GetImages(manager);
            SearchAndMatch(imageEnum, report, verbose, mode);
        }

        private static bool TestImage(ImageProxy image)
        {
            return image.TryLoadImageInfo();
        }

        private void PreprocessVerboseParallel(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles)
        {
            var validCount = 0;
            var localImageList = new List<ImageProxy>();
            var localInvalidFiles = new List<FileInfo>();
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = PreprocessParallelism
            };

            "Collecting files ... ".InplaceWrite();
            var tempList = imageEnum.ToList();
            var totalCount = tempList.Count;
            $"{totalCount} file(s) collected.".InplaceConcludeWriteLine();

            Parallel.ForEach(tempList, options, (image) =>
            {
                if (TestImage(image))
                {
                    Assert(image != null);
                    localImageList.Add(image);
                    lock (localImageList)
                    {
                        validCount++;
                        var invalidCount = localInvalidFiles.Count;
                        $"{validCount} valid and {invalidCount} invalid file(s) analysed out of {totalCount}. Recent: {image.File.Name}.".InplaceWrite();
                    }
                }
                else
                {
                    lock (localInvalidFiles)
                    {
                        localInvalidFiles.Add(image.File);
                        var invalidCount = localInvalidFiles.Count;
                        $"{validCount} valid and {invalidCount} invalid file(s) analysed out of {totalCount}. Recent: {image.File.Name}.".InplaceWrite();
                    }
                }
            });
            imageList = localImageList;
            invalidFiles = localInvalidFiles;
        }

        private static void PreprocessSequential(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles, bool verbose)
        {
            var validCount = 0;
            var removeList = new List<int>();

            "Collecting files ... ".InplaceWrite();
            imageList = imageEnum.ToList();
            var totalCount = imageList.Count;
            $"{totalCount} file(s) collected.".InplaceConcludeWriteLine();

            for (var i = 0; i < imageList.Count; i++)
            {
                var image = imageList[i];
                if (TestImage(image))
                {
                    imageList.Add(image);
                    validCount++;
                    if (verbose)
                    {
                        $"{validCount}/{totalCount} file(s) collected. Recent: {image.File.Name}.".InplaceWrite();
                    }
                }
                else
                {
                    removeList.Add(i);
                    if (verbose)
                    {
                        $"{validCount}/{totalCount} file(s) collected. Last ignored:    {image.File.Name}.".InplaceWrite();
                    }
                }
            }

            var temp = imageList;
            invalidFiles = removeList.Select(i => temp[i].File).ToList();
            foreach (var i in ((IEnumerable<int>)removeList).Reverse())
            {
                imageList.RemoveAt(i);
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
                Assert(a != null && b != null);
                return a.AbsAspRatio.CompareTo(b.AbsAspRatio);
            });
            Console.WriteLine("Files sorted...");
        }

        private void PreprocessSilentParallel(IEnumerable<ImageProxy> imageEnum, out List<ImageProxy> imageList, out List<FileInfo> invalidFiles)
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

        private static void ShowMatches(string reportFile, IList<MatchResult> matchRes, ICollection<FileInfo> invalidFiles, bool verbose)
        {
            if (reportFile != null)
            {
                if (verbose) Console.WriteLine("Exporting matched images...");
                using (var reportWriter = new StreamWriter(reportFile))
                {
                    var i = 0;
                    for (; i < matchRes.Count; i++)
                    {
                        var res = matchRes[i];
                        var match = res as ImagesMatch;
                        if (match == null) break;
                        var mrep = $"{match.Image1.Path}|{match.Image2.Path}:{match.Mse}";
                        reportWriter.WriteLine(mrep);
                    }

                    if (i < matchRes.Count)
                    {
                        reportWriter.WriteLine(">>>>>>>>>> Comp Failures >>>>>>>>>>");
                        for (; i < matchRes.Count; i++)
                        {
                            var res = (MatchError)matchRes[i];
                            var mrep = $"{res.Image1.Path}|{res.Image2.Path}:{res.GetErrorDescription()}";
                            reportWriter.WriteLine(mrep);
                        }
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
                foreach (var match in matchRes.OfType<ImagesMatch>())
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

        private static IList<MatchResult> MatchImages(IList<ImageProxy> imageList, Modes mode, bool verbose)
        {
            List<MatchResult> matches;
            if (verbose)
            {
                var modeName = GetModeName(mode);
                Console.WriteLine("Matching image files in " + modeName + " mode ...");

                Common.ResetInPlaceWriting();

                var total = ((long)imageList.Count - 1) * imageList.Count / 2;
                long tasks = 0;
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
                        }).ToList();
                        break;
                    case Modes.Sequential:
                        matches = imageList.SimpleSearchAndMatchImages((i, j) =>
                        {
                            tasks++;
                            Common.PrintProgress(tasks, total);
                        }).ToList();
                        break;
                    case Modes.SequentialHasty:
                        matches = imageList.SimpleSearchAndMatchImagesHasty((i, j) =>
                        {
                            tasks++;
                            Common.PrintProgress(tasks, total);
                        }).ToList();
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
                        matches = imageList.SimpleSearchAndMatchImagesParallel().ToList();
                        break;
                    case Modes.Sequential:
                        matches = imageList.SimpleSearchAndMatchImages().ToList();
                        break;
                    case Modes.SequentialHasty:
                        matches = imageList.SimpleSearchAndMatchImagesHasty().ToList();
                        break;
                    default:
                        throw new ArgumentException("Unexpected image search and match mode");
                }
            }
            matches.Sort(CompareMatchResults);
            return matches;
        }

        private void SearchAndMatch(IEnumerable<ImageProxy> imageEnum, string reportFile, bool verbose = false, Modes mode = Modes.Sequential)
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
