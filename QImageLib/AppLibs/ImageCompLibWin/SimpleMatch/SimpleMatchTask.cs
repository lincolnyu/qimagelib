using System.Collections.Generic;
using QImageLib.Matcher;
using ImageCompLibWin.Data;

namespace ImageCompLibWin.SimpleMatch
{
    public class SimpleMatchTask : Task
    {
        #region Fields

        private ICollection<IResource> _requiredResources;

        #endregion

        #region Constructors

        public SimpleMatchTask(ImageProxy image1, ImageProxy image2, ImageManager manager) : base(manager?.TaskManager)
        {
            _requiredResources = new ImageProxy[] {
                image1,
                image2
            };
            ImageManager = manager;
            Image1 = image1;
            Image2 = image2;
        }

        public SimpleMatchTask(ImageProxy image1, ImageProxy image2, double mseThr) : this (image1, image2, image1.ImageManager)
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

        public SimpleImageMatch Result { get; private set; }

        #endregion

        #region Methods

        #region Task members

        protected override void Perform()
        {
            var mse = Image1.YImage.GetSimpleMinMse(Image2.YImage, MseThr);
            if (mse != null)
            {
                Result = new SimpleImageMatch(Image1, Image2, mse.Value);
            }
        }

        #endregion

        #endregion
    }
}
