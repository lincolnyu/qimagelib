using System.Collections.Generic;
using QImageLib.Matcher;
using ImageCompLibWin.Data;
using ImageCompLibWin.Tasking;
using static ImageCompLibWin.SimpleMatch.MatchResults;
using QImageLib.Images;

namespace ImageCompLibWin.SimpleMatch
{
    public class SimpleMatchTask : Task
    {
        #region Fields

        private ICollection<IResource> _requiredResources;

        #endregion

        #region Constructors

        public SimpleMatchTask(ImageProxy image1, ImageProxy image2, double mseThr, ImageManager manager) : base(manager?.TaskManager)
        {
            _requiredResources = new ImageProxy[] {
                image1,
                image2
            };
            ImageManager = manager;
            Image1 = image1;
            Image2 = image2;
            MseThr = mseThr;
        }

        public SimpleMatchTask(ImageProxy image1, ImageProxy image2, double mseThr) : this (image1, image2, mseThr, image1.ImageManager)
        {
        }

        #endregion

        #region Properties

        #region Task members

        public override ICollection<IResource> RequiredResources => _requiredResources;

        #endregion

        public ImageManager ImageManager { get; }

        public ImageProxy Image1 { get; }

        public ImageProxy Image2 { get; }

        public double MseThr { get; }

        public MatchResult Result { get; private set; }

        #endregion

        #region Methods

        #region Task members

        protected override void Perform()
        {
            YImage y1, y2;
            var error = ImageSearchAndMatch.CheckYImages(Image1, Image2, out y1, out y2);
            if (error != null)
            {
                Result = error;
                return;
            }

            var mse = y1.GetSimpleMinMse(y2, MseThr);
            if (mse != null)
            {
                Result = new ImagesMatch(Image1, Image2, mse.Value);
            }
        }

        #endregion

        #endregion
    }
}
