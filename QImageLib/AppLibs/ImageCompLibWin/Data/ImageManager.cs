using ImageCompLibWin.Tasking;
using ImageCompLibWin.SimpleMatch;

namespace ImageCompLibWin.Data
{
    public class ImageManager
    {
        public const int DefaultThumbSize = 64;

        public const int DefaultMaxYConvAttempts = 3;

        public ImageManager(TaskManager taskManager)
        {
            TaskManager = taskManager;
        }
       
        public static ImageManager Instance { get; } = new ImageManager(TaskManager.Instance);

        public TaskManager TaskManager { get; }

        public int MaxYConvAttempts { get; } = DefaultMaxYConvAttempts;

        public double MseThr { get; } = ImageComp.DefaultMseThr;
        public int ThumbSize { get; } = DefaultThumbSize;
    }
}
