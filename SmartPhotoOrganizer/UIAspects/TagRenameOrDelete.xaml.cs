using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for TagRenameOrDelete.xaml
    /// </summary>
    public partial class TagRenameOrDelete
    {
        public TagRenameOrDelete()
        {
            InitializeComponent();
            PhotoManager.Config.ApplyWindowSize("TagRenameOrDelete", this);
            UpdateTagList(string.Empty);
        }

        private void UpdateTagList()
        {
            UpdateTagList(SearchBox.Text.ToLowerInvariant());
        }

        private void UpdateTagList(string searchString)
        {
            var tagsSummary = Database.GetTagsSummary(PhotoManager.Connection);

            if (searchString == string.Empty)
            {
                var selectedTags = from pair in tagsSummary orderby pair.Value descending select new TagWithFrequency { Tag = pair.Key, Frequency = pair.Value };
                TagBox.ItemsSource = selectedTags;
            }
            else
            {
                var selectedTags = from pair in tagsSummary where pair.Key.Contains(searchString) orderby pair.Value descending select new TagWithFrequency { Tag = pair.Key, Frequency = pair.Value };
                TagBox.ItemsSource = selectedTags;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PhotoManager.Config.SaveWindowSize("TagRenameOrDelete", this);
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTagList();
        }

        private void tagBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagBox.SelectedIndex >= 0)
            {
                RenameButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
            else
            {
                RenameButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
        }

        private void renameButton_Click(object sender, RoutedEventArgs e)
        {
            var oldTag = ((TagWithFrequency) TagBox.SelectedItem).Tag;
            var newTag = RenameBox.Text.ToLowerInvariant();

            if (newTag.Contains(' ') || newTag.Contains('|'))
            {
                MessageBox.Show("Cannot rename tag. Tag cannot contain ' ' or '|' characters.");
                return;
            }
            if (newTag == string.Empty)
            {
                MessageBox.Show("New tag cannot be blank.");
                return;
            }
            if (MessageBox.Show("Are you sure you want to rename this tag?", "Confirm rename", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                TagOperations.RenameTag(oldTag, newTag);
                UpdateTagList();
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var tagToDelete = ((TagWithFrequency) TagBox.SelectedItem).Tag;

            if (MessageBox.Show("Are you sure you want to remove this tag from all of your images?", "Confirm delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                TagOperations.DeleteTag(tagToDelete);
                UpdateTagList();
            }
        }
    }
}
