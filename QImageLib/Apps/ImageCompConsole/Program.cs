using System;
using System.Linq;
using System.Reflection;
using ImageCompConsole.Subprograms;
using QLogger.AppHelpers;

namespace ImageCompConsole
{
    class Program
    {
        static Subprogram[] Subprograms =
        {
            ImageSimpleComp.Instance,
            SearchImageFiles.Instance,
            ImageSimpleSearchAndMatch.Instance
        };

        private static void Main(string[] args)
        {
            try
            {
                if (args.Contains("--help"))
                {
                    PrintUsage();
                    return;
                }
                foreach (var subprog in Subprograms)
                {
                    if (subprog.Subcommand == args[0])
                    {
                        subprog.Run(args);
                        break;
                    }
                }      
            }
            catch (Exception)
            {
                Console.WriteLine($"Invalid command arguments. See the usage below.");
                Console.WriteLine();
                PrintUsage();
            }
        }

        private static void PrintUsage()
        {
            var appname = AppInfo.GetAppExecutableName();
            var ver = AppInfo.GetAppVersion();
            Console.WriteLine($"ImageComp Tool for Windows [Version {ver.Major}.{ver.Minor}] -- based on QImageLib");

            Console.WriteLine();
            foreach (var subprog in Subprograms)
            {
                subprog.PrintUsage(appname, 2, 2);
                Console.WriteLine();
            }

            Console.WriteLine("  To show this usage help: ");
            Console.WriteLine("    " + appname + " --help");
        }
    }
}
