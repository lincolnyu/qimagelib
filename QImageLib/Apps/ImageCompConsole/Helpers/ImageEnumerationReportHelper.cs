using System.IO;
using System.Linq;
using static ImageCompLibWin.Helpers.ImageEnumeration;

namespace ImageCompConsole.Helpers
{
    public static class ImageEnumerationReportHelper
    {
        public static void WriteReport(this ImageEnumErrors errors, StreamWriter sw)
        {
            sw.WriteLine(">>>>>>>>>> Directories or files with access errors >>>>>>>>>>");
            foreach (var e in errors.Errors)
            {
                var item = e.Value;
                sw.WriteLine($"{item.Location} : {item.Errors.First().Exception.Message}");
            }
        }
    }
}
