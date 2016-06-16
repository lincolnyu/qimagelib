using System;
using System.Drawing;
using ImageCompLibWin.SimpleMatch;

namespace ImageCompConsole.Subprograms
{
    class ImageSimpleComp : Subprogram
    {
        public static ImageSimpleComp Instance { get; } = new ImageSimpleComp();

        public override string Subcommand { get; } = "c";

        public override void PrintUsage(string appname, int indent, int contentIndent)
        {
            var indentStr = new string(' ', indent);
            var contentIndentStr = new string(' ', indent + contentIndent);
            Console.WriteLine(indentStr + "To compare two images: ");
            Console.WriteLine(contentIndentStr + LeadingCommandString(appname) + " <path to first image> <path to second image>");
        }

        public override void Run(string[] args)
        {
            var path1 = args[1];
            var path2 = args[2];
            Comp(path1, path2);
        }

        public static void Comp(string path1, string path2)
        {
            try
            {
                var bmp1 = (Bitmap)Image.FromFile(path1);
                var bmp2 = (Bitmap)Image.FromFile(path2);
                var mse = bmp1.GetSimpleMinMse(bmp2);
                var similar = mse <= ImageComp.DefaultMseThr;
                var ssim = similar ? "similar" : "different";
                Console.WriteLine($"Simple min MSE = {mse}, images considered {ssim}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong. Details: {e.Message}");
            }
        }

    }
}
