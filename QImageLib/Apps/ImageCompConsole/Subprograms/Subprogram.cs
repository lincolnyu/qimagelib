namespace ImageCompConsole.Subprograms
{
    abstract class Subprogram
    {
        public abstract string Subcommand { get; }
        public abstract void PrintUsage(string appname, int indent, int contentIndent);
        public abstract void Run(string[] args);

        protected string LeadingCommandString(string appname)
        {
            return appname + " " + Subcommand;
        }
    }
}
