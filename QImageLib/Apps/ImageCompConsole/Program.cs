using System;
using System.Linq;
using ImageCompConsole.Subprograms;
using QLogger.AppHelpers;
using QLogger.ConsoleHelpers;
using QLogger.Shell;

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
                if (args.Length == 0)
                {
                    RunInteractive();
                    return;
                }
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
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong:");
                Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                Console.WriteLine("Try the following command for usage or email linc.yu@outlook.com for support.");
                var appname = AppInfo.GetAppExecutableName();
                Console.WriteLine($"  {appname} --help");
            }
        }

        private static void RunInteractive()
        {
            PrintInteractiveWelcome();

        }

        private static void PrintUsage()
        {
            var appname = AppInfo.GetAppExecutableName();
            var ver = AppInfo.GetAppVersion();
            Console.WriteLine($"ImageComp Tool for Windows [Version {ver.Major}.{ver.Minor}] -- based on QImageLib");

            Console.WriteLine();
            foreach (var subprog in Subprograms)
            {
                var leadingStr = subprog.LeadingCommandString(appname);
                subprog.PrintUsage(leadingStr, 2, 2);
                Console.WriteLine();
            }

            Console.WriteLine("  To show this usage help: ");
            Console.WriteLine($"    {appname } --help");
        }

        private static void PrintInteractiveWelcome()
        {
            var appname = AppInfo.GetAppExecutableName();
            var ver = AppInfo.GetAppVersion();
            Console.WriteLine($"ImageComp Tool for Windows [Version {ver.Major}.{ver.Minor}] -- based on QImageLib (Interactive Mode)");
            Console.WriteLine("Type in 'help' for usage details.");
            var running = true;
            var consoleContext = WindowsConsoleContext.Instance;
            var commandSystem = new WindowsCommandSystem(consoleContext);
            consoleContext.ResetCurrentDirectory();
            while (running)
            {
                Console.Write($"{consoleContext.CurrentDirectory}>");
                var cmd = Console.ReadLine().Trim();
                var cmdlc = cmd.ToLower();
                if (cmdlc == "help")
                {
                    PrintInteractiveUsage();
                }
                else if (cmdlc == "quit" || cmdlc == "exit")
                {
                    running = false;
                }
                else
                {
                    var executed = false;
                    var args = cmd.ParseArgs().ToArray();
                    if (args.Length > 0)
                    {
                        foreach (var subprog in Subprograms)
                        {
                            if (subprog.Subcommand == args[0])
                            {
                                executed = true;
                                subprog.Run(args);
                                break;
                            }
                        }
                        if (!executed)
                        {
                            var result = commandSystem.ExecuteCommand(args);
                            if (result != WindowsCommandSystem.Results.Success)
                            {
                                var msg = WindowsCommandSystem.ResultToString(result);
                                Console.WriteLine(msg);
                            }
                            else
                            {
                                executed = true;
                            }
                        }
                        if (!executed)
                        {
                            Console.WriteLine("Type 'help' for usage details.");
                        }
                    }
                }
            }
        }

        private static void PrintInteractiveUsage()
        {
            foreach (var subprog in Subprograms)
            {
                var leadingStr = subprog.LeadingCommandStringInteractiveMode();
                 subprog.PrintUsage(leadingStr, 2, 2);
                Console.WriteLine();
            }

            Console.WriteLine("  To show this usage help use 'help' command");
            Console.WriteLine("  To quit this application use 'quit' or 'exit' command");
        }
    }
}
