using System.Windows;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for EditMultipleTags.xaml
    /// </summary>
    public partial class EditMultipleTags
    {
        private readonly TagAutoCompleteTextBox _tagBox;

        public EditMultipleTags(MainWindow window)
        {
            Owner = window;
            InitializeComponent();
            var tagsSummary = Database.GetTagsSummary(PhotoManager.Connection);
            _tagBox = new TagAutoCompleteTextBox
            {
                Margin = new Thickness(46, 45, 17, 0),
                VerticalAlignment = VerticalAlignment.Top,
                TagsSummary = tagsSummary,
                LimitToOneTag = true
            };
            MainGrid.Children.Add(_tagBox);
            TitleText.Text = "Editing tags on " + ImageListControl.TotalImages + " images.";
            _tagBox.TextBox.Focus();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            TagOperations.AddTagToVisible(_tagBox.Text.ToLowerInvariant());
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            TagOperations.RemoveTagFromVisible(_tagBox.Text.ToLowerInvariant());
        }
    }
}