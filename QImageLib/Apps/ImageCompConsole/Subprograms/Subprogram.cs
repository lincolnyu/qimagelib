namespace ImageCompConsole.Subprograms
{
    abstract class Subprogram
    {
        public abstract string Subcommand { get; }
        public abstract void PrintUsage(string leadingStr, int indent, int contentIndent);
        public abstract void Run(string[] args);

        public string LeadingCommandString(string appname)
        {
            return appname + " " + Subcommand;
        }

        public string LeadingCommandStringInteractiveMode()
        {
            return Subcommand;
        }
    }
}
