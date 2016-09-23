using System.Windows.Media;

namespace SmartPhotoOrganizer.UIAspects
{
    public static class SharedUi
    {
        public static Brush SelectColor { get; } = new SolidColorBrush(Color.FromRgb(222, 235, 253));
        public static Brush HoverColor { get; } = new SolidColorBrush(Color.FromRgb(239, 248, 253));
    }
}
