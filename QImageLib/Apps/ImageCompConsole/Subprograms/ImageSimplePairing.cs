using ImageCompConsole.Global;
using ImageCompConsole.Helpers;
using ImageCompLibWin.Data;
using ImageCompLibWin.Helpers;
using ImageCompLibWin.SimpleMatch;
using ImageCompLibWin.Tasking;
using QLogger.ConsoleHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ImageCompLibWin.Helpers.ImageEnumeration;
using static ImageCompLibWin.SimpleMatch.MatchResults;
using static System.Diagnostics.Debug;

namespace ImageCompConsole.Subprograms
{
    class ImageSimplePairing : MatchingProgram
    {
        public static ImageSimplePairing Instance { get; } = new ImageSimplePairing();

        public ImageManager ImageManager { get; private set; }

        public override string Subcommand { get; } = "sm";

        public override void PrintUsage(string leadingStr, int indent, int contentIndent)
        {
            var indentStr = new string(' ', indent);
            var contentIndentStr = new string(' ', indent + contentIndent);
            Console.WriteLine(indentStr + $"To search for similar images in the direcory and its subdirectories (-q to turn on quite mode, -p to run parallel, -h to use sequential hasty mode, -s to specify the maximum total size in pixels of images processed in memory (default {TaskManager.DefaultQuota.ConvertToM()}M pixels), -pp to specify the number of parallel tasks for image file loading and preprocessing (default {DefaultPreprocessParallelism}))");
            Console.WriteLine(contentIndentStr + leadingStr + " {<base directory>|[-l] <list file>} [-p|-h] [-q] [-o <report file>] [-s <total pixel number>] [-pp <num concurrent tasks>]");
        }

        public override void Run(string[] args)
        {
            var report = args.GetSwitchValue("-o");
            var verbose = !args.Contains("-q");
            var listfile = args.GetSwitchValue("-l");
            var hasty = args.Contains("-h");
            var quotastr = args.GetSwitchValue("-s");
            int quota;
            if (!int.TryParse(quotastr, out quota))
            {
                quota = TaskManager.DefaultQuota;
            }
            var taskManager = new TaskManager(quota);
            ImageManager = new ImageManager(taskManager);

            LoadBasicFromArgs(args);

            if (listfile != null)
            {
                SearchAndMatchInList(listfile, report, verbose);
            }
            else
            {
                var dir = args[1];
                SearchAndMatchInDir(dir, report, verbose);
            }
        }

        private void SearchAndMatchInList(string listfile, string report, bool verbose)
        {
            var errors = new ImageEnumErrors();
            var imageEnum = GetImagesFromListFile(listfile).GetImages(errors, ImageManager);
            SearchAndMatch(imageEnum, report, verbose);
            ShowImageEnumErrors(errors, report, verbose);
        }

        private void SearchAndMatchInDir(string sdir, string report, bool verbose = false)
        {
            var dir = new DirectoryInfo(sdir);
            var errors = new ImageEnumErrors();
            var imageEnum = dir.GetImages(errors, ImageManager);
            SearchAndMatch(imageEnum, report, verbose);
            ShowImageEnumErrors(errors, report, verbose);
        }
        
        private IList<MatchResult> MatchImages(IList<ImageProxy> imageList, bool verbose)
        {
            List<MatchResult> matches;
            if (verbose)
            {
                var modeName = GetModeName();
                Console.WriteLine("Matching image files in " + modeName + " mode ...");

                Common.ResetInPlaceWriting();

                var total = ((long)imageList.Count - 1) * imageList.Count / 2;
                long tasks = 0;
                Common.StartProgress();
                switch (MatchingMode)
                {
                    case MatchingModes.Parallel:
                        matches = imageList.SimpleSearchAndMatchImagesParallel(() =>
                        {
                            lock (imageList)
                            {
                                tasks++;
                                Common.PrintProgress(tasks, total);
                            }
                        }).ToList();
                        break;
                    case MatchingModes.Sequential:
                        matches = imageList.SimpleSearchAndMatchImages((i, j) =>
                        {
                            tasks++;
                            Common.PrintProgress(tasks, total);
                        }).ToList();
                        break;
                    case MatchingModes.SequentialHasty:
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
                switch (MatchingMode)
                {
                    case MatchingModes.Parallel:
                        matches = imageList.SimpleSearchAndMatchImagesParallel().ToList();
                        break;
                    case MatchingModes.Sequential:
                        matches = imageList.SimpleSearchAndMatchImages().ToList();
                        break;
                    case MatchingModes.SequentialHasty:
                        matches = imageList.SimpleSearchAndMatchImagesHasty().ToList();
                        break;
                    default:
                        throw new ArgumentException("Unexpected image search and match mode");
                }
            }
            matches.Sort(CompareMatchResults);
            return matches;
        }

        private void SearchAndMatch(IEnumerable<ImageProxy> imageEnum, string reportFile, bool verbose = false)
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
                ProcessImagesToList(imageEnum, reportFile, verbose, out imageList, out invalidFiles);
                var matches = MatchImages(imageList, verbose);
                ShowMatches(reportFile, matches, invalidFiles, verbose);
            }
            catch (Exception e)
            {
                Common.PrintException(e, verbose);
            }
        }
    }
}
