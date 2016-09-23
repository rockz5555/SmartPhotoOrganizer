using System.Windows.Controls;

namespace SmartPhotoOrganizer.DataStructures
{
    public class CachedImage
    {
        public ImageViewer Image { get; set; }
        public bool CallInProgress { get; set; }
        public Image UiImage { get; set; }
    }
}
