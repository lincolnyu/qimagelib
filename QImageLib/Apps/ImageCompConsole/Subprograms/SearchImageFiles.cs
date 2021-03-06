﻿using System;
using System.IO;
using System.Linq;
using ImageCompConsole.Global;
using ImageCompLibWin.Data;
using ImageCompLibWin.Helpers;
using QLogger.ConsoleHelpers;
using static ImageCompLibWin.Helpers.ImageEnumeration;
using ImageCompConsole.Helpers;

namespace ImageCompConsole.Subprograms
{
    class SearchImageFiles : Subprogram
    {
        public static SearchImageFiles Instance { get; } = new SearchImageFiles();

        public override string Subcommand { get; } = "s";

        public override void PrintUsage(string leadingStr, int indent, int contentIndent)
        {
            var indentStr = new string(' ', indent);
            var contentIndentStr = new string(' ', indent + contentIndent);
            Console.WriteLine(indentStr + "To find out all images files in the directory and its subdirectory (-c to check image for processability)");
            Console.WriteLine(contentIndentStr + leadingStr + " <base directory> [-c] [-o <report file>]");
        }

        public override void Run(string[] args)
        {
            var dir = args[1];
            var report = args.GetSwitchValue("-o");
            var verbose = !args.Contains("-q");
            var check = args.Contains("-c");
            Search(dir, report, check, verbose);
        }

        public static void Search(string sdir, string report, bool check, bool verbose)
        {
            var dir = new DirectoryInfo(sdir);
            var errors = new ImageEnumErrors();
            var imageEnum = dir.GetImageFiles(errors);
            if (check)
            {
                imageEnum = imageEnum.Where(x => new ImageProxy(x).TryLoadImageInfo());
            }
            if (verbose)
            {
                Console.WriteLine("Collecting image files...");
            }
            var imageFiles = imageEnum.ToList();
            var exportReport = !string.IsNullOrWhiteSpace(report);
            if (verbose && exportReport)
            {
                Common.ResetInPlaceWriting();
                var count = 0;
                string lastFile = null;
                foreach (var imageFile in imageFiles)
                {
                    count++;
                    $"{count} file(s) found, last being {imageFile.Name}".InplaceWrite();
                    lastFile = imageFile.Name;
                }
                $"{count} file(s) found, last being {lastFile}".InplaceWrite(true);
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
                Console.WriteLine($"Image collection completed. {errors.Errors.Count} directories or files failed to access and ignored.");
            }

            if (exportReport)
            {
                using (var sw = new StreamWriter(report))
                {
                    foreach (var imageFile in imageFiles)
                    {
                        sw.WriteLine(imageFile.FullName);
                    }
                    errors.WriteReport(sw);
                }
                if (verbose)
                {
                    Console.WriteLine($"List of image files is exported to '{report}'");
                }
            }
        }
    }
}
