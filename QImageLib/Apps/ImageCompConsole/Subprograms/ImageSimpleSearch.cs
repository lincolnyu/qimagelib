using ImageCompConsole.Helpers;
using ImageCompLibWin.Data;
using ImageCompLibWin.Tasking;
using QLogger.ConsoleHelpers;
using System;
using System.IO;
using System.Linq;
using static ImageCompLibWin.Helpers.ImageEnumeration;
using System.Collections.Generic;
using ImageCompConsole.Global;
using static ImageCompLibWin.SimpleMatch.MatchResults;
using ImageCompLibWin.SimpleMatch;

namespace ImageCompConsole.Subprograms
{
    class ImageSimpleSearch : MatchingProgram
    {
        public static ImageSimpleSearch Instance { get; } = new ImageSimpleSearch();

        public override string Subcommand { get; } = "ss";

        public ImageManager ImageManager { get; private set; }

        public override void PrintUsage(string leadingStr, int indent, int contentIndent)
        {
            var indentStr = new string(' ', indent);
            var contentIndentStr = new string(' ', indent + contentIndent);
            Console.WriteLine(indentStr + $"To match images between the left (--leftlist for list or --left for file/directory) and the right (--rightlist for list or --right for file/directory) (-q to turn on quite mode, -p to run parallel, -s to specify the maximum total size in pixels of images processed in memory (default {TaskManager.DefaultQuota.ConvertToM()}M pixels), -pp to specify the number of parallel tasks for image file loading and preprocessing (default {DefaultPreprocessParallelism}))");
            Console.WriteLine(contentIndentStr + leadingStr + " {[--leftlist] <left list>|[--left] <file or directory on the left>} {[--rightlist] <right list>|[--right] <file or directory on the right>} [-p] [-q] [-o <report file>] [-s <total pixel number>] [-pp <num concurrent tasks>]");
        }

        public override void Run(string[] args)
        {
            var leftlist = args.GetSwitchValue("--leftlist");
            var rightlist = args.GetSwitchValue("--rightlist");
            var left = args.GetSwitchValue("--left");
            var right = args.GetSwitchValue("--right");

            var report = args.GetSwitchValue("-o");
            var verbose = !args.Contains("-q");
            var quotastr = args.GetSwitchValue("-s");
            int quota;
            if (!int.TryParse(quotastr, out quota))
            {
                quota = TaskManager.DefaultQuota;
            }
            var taskManager = new TaskManager(quota);
            ImageManager = new ImageManager(taskManager);

            LoadBasicFromArgs(args);

            var errors = new ImageEnumErrors();
            IEnumerable<ImageProxy> leftEnum, rightEnum;
            if (leftlist != null)
            {
                leftEnum = GetImagesFromListFile(leftlist).GetImages(errors, ImageManager);
            }
            else if (left != null)
            {
                if (File.Exists(left))
                {
                    leftEnum = new string[] { left }.GetImages(errors, ImageManager);
                }
                else if (Directory.Exists(left))
                {
                    var dir = new DirectoryInfo(left);
                    leftEnum = dir.GetImages(errors, ImageManager);
                }
                else
                {
                    Console.WriteLine("Cannot locate the file or directory on the left");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Left not specified");
                return;
            }
            if (rightlist != null)
            {
                rightEnum = GetImagesFromListFile(rightlist).GetImages(errors, ImageManager);
            }
            else if (right != null)
            {
                if (File.Exists(right))
                {
                    rightEnum = new string[] { right }.GetImages(errors, ImageManager);
                }
                else if (Directory.Exists(right))
                {
                    var dir = new DirectoryInfo(right);
                    rightEnum = dir.GetImages(errors, ImageManager);
                }
                else
                {
                    Console.WriteLine("Cannot locate the file or directory on the right");
                    return;
                }
            }
            else
            {
                Console.WriteLine("right not specified");
                return;
            }

            SearchAndMatch(leftEnum, rightEnum, report, verbose);
            ShowImageEnumErrors(errors, report, verbose);
        }

        private IList<MatchResult> MatchImages(IList<ImageProxy> leftList, IList<ImageProxy> rightList, bool verbose)
        {
            List<MatchResult> matches;
            if (verbose)
            {
                var modeName = GetModeName();
                Console.WriteLine("Matching image files in " + modeName + " mode ...");

                Common.ResetInPlaceWriting();
                var total = leftList.Count * rightList.Count;
                long tasks = 0;
                Common.StartProgress();
                switch (MatchingMode)
                {
                    case MatchingModes.Parallel:
                        matches = ImageSearchAndMatch.SimpleSearchAndMatchImagesParallel(leftList, rightList, () =>
                        {
                            lock (leftList)
                            {
                                tasks++;
                                Common.PrintProgress(tasks, total);
                            }
                        }).ToList();
                        break;
                    case MatchingModes.Sequential:
                        matches = ImageSearchAndMatch.SimpleSearchAndMatchImages(leftList, rightList, (i, j) =>
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
                        matches = ImageSearchAndMatch.SimpleSearchAndMatchImagesParallel(leftList, rightList).ToList();
                        break;
                    case MatchingModes.Sequential:
                        matches = ImageSearchAndMatch.SimpleSearchAndMatchImages(leftList, rightList).ToList();
                        break;
                    default:
                        throw new ArgumentException("Unexpected image search and match mode");
                }
            }
            return matches;
        }

        private void SearchAndMatch(IEnumerable<ImageProxy> leftEnum, IEnumerable<ImageProxy> rightEnum, string report, bool verbose)
        {
            try
            {
                List<ImageProxy> leftList, rightList;
                List<FileInfo> leftInvalid, rightInvalid;
                ProcessImagesToList(leftEnum, report, verbose, out leftList, out leftInvalid);
                ProcessImagesToList(rightEnum, report, verbose, out rightList, out rightInvalid);
                var matches = MatchImages(leftList, rightList, verbose);
                var invalidFiles = leftInvalid.Concat(rightInvalid).ToList();
                ShowMatches(report, matches, invalidFiles, verbose);
            }
            catch (Exception e)
            {
                Common.PrintException(e, verbose);
            }
        }
    }
}
