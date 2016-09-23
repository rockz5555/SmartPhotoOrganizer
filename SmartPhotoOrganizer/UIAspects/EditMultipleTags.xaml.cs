using System.Windows;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for EditMultipleTags.xaml
    /// </summary>
    public partial class EditMultipleTags
    {
        private TagAutoCompleteTextBox tagBox;

        public EditMultipleTags(MainWindow window)
        {
            Owner = window;
            InitializeComponent();
            var tagsSummary = Database.GetTagsSummary(PhotoManager.Connection);
            tagBox = new TagAutoCompleteTextBox
            {
                Margin = new Thickness(46, 45, 17, 0),
                VerticalAlignment = VerticalAlignment.Top,
                TagsSummary = tagsSummary,
                LimitToOneTag = true
            };
            MainGrid.Children.Add(tagBox);
            TitleText.Text = "Editing tags on " + ImageListControl.TotalImages + " images.";
            tagBox.TextBox.Focus();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            TagOperations.AddTagToVisible(tagBox.Text.ToLowerInvariant());
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            TagOperations.RemoveTagFromVisible(tagBox.Text.ToLowerInvariant());
        }
    }
}