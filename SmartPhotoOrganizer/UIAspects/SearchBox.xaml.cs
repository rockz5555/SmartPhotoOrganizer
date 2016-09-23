using System.Windows;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for SearchBox.xaml
    /// </summary>
    public partial class SearchBox
    {
        private readonly TagAutoCompleteTextBox _tagBox;

        public SearchBox(MainWindow mainWindow)
        {
            Owner = mainWindow;
            InitializeComponent();
            var tagsSummary = Database.GetTagsSummary(PhotoManager.Connection);
            _tagBox = new TagAutoCompleteTextBox
            {
                Margin = new Thickness(23, 10, 22, 0),
                TagsSummary = tagsSummary,
                AutoCompleteEnabled = PhotoManager.SearchTags,
                IgnoreDashPrefix = true
            };
            RootGrid.Children.Add(_tagBox);
            _tagBox.AutoCompleteEnabled = PhotoManager.SearchTags;
            var currentSearch = ImageQuery.Search;
            _tagBox.TextBox.Text = currentSearch;
            _tagBox.TextBox.SelectAll();
            SearchFileName.IsChecked = PhotoManager.SearchFileName;
            SearchTags.IsChecked = PhotoManager.SearchTags;
            _tagBox.TextBox.Focus();
        }

        private void searchOk_Click(object sender, RoutedEventArgs e)
        {
            QueryOperations.RunSearch(_tagBox.TextBox.Text.Trim(), SearchFileName.IsChecked != null && (bool) SearchFileName.IsChecked, SearchTags.IsChecked != null && (bool) SearchTags.IsChecked);
            DialogResult = true;
        }

        private void searchTags_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTags.IsChecked != null) _tagBox.AutoCompleteEnabled = (bool) SearchTags.IsChecked;
        }
    }
}
