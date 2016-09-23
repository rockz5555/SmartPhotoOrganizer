using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for EditViewList.xaml
    /// </summary>
    public partial class EditViewList
    {
        private readonly int _listId;
        private readonly int _listIndex;
        private readonly List<string> _orders;
        private readonly TagAutoCompleteTextBox _searchBox;

        public EditViewList(int listId, int listIndex)
        {
            InitializeComponent();
            _orders = new List<string> {"random", "name", "lastWriteTime"};
            _listId = listId;
            _listIndex = listIndex;
            OrderByBox.SelectionChanged += orderByBox_SelectionChanged;
            var tagsSummary = Database.GetTagsSummary(PhotoManager.Connection);
            _searchBox = new TagAutoCompleteTextBox
            {
                Margin = new Thickness(70, 112, 170, 0),
                VerticalAlignment = VerticalAlignment.Top,
                TagsSummary = tagsSummary,
                IgnoreDashPrefix = true
            };
            MainGrid.Children.Add(_searchBox);
            if (listId < 0) return;
            var command = new SQLiteCommand("SELECT name, orderBy, ascending, rating, searchString, searchTags, searchFileName, untagged, customClause FROM viewLists WHERE id = " + _listId, PhotoManager.Connection);
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                NameBox.Text = reader.GetString("name");
                OrderByBox.SelectedIndex = _orders.IndexOf(reader.GetString("orderBy"));
                AscendingBox.SelectedIndex = reader.GetBoolean("ascending") ? 0 : 1;
                var ratingIndex = reader.GetInt32("rating");
                if (ratingIndex < 0)
                {
                    ratingIndex = 6;
                }
                RatingBox.SelectedIndex = ratingIndex;
                TagsSearchBox.IsChecked = reader.GetBoolean("searchTags");
                FileNameSearchBox.IsChecked = reader.GetBoolean("searchFileName");
                UntaggedBox.IsChecked = reader.GetBoolean("untagged");
                CustomClauseBox.Text = reader.GetString("customClause");
                _searchBox.AutoCompleteEnabled = reader.GetBoolean("searchTags");
                _searchBox.Text = reader.GetString("searchString");
            }
        }

        private void orderByBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AscendingBox.IsEnabled = OrderByBox.SelectedIndex != _orders.IndexOf("random");
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection connection = PhotoManager.Connection;

            if (NameBox.Text == "")
            {
                MessageBox.Show("Must specify a name.");
                return;
            }

            var name = NameBox.Text;
            var orderBy = _orders[OrderByBox.SelectedIndex];
            var ascending = AscendingBox.SelectedIndex == 0;
            var rating = RatingBox.SelectedIndex;
            if (rating > 5)
            {
                rating = -1;
            }

            var searchString = _searchBox.Text;
            var searchTags = TagsSearchBox.IsChecked != null && (bool) TagsSearchBox.IsChecked;
            var searchFileName = FileNameSearchBox.IsChecked != null && (bool) FileNameSearchBox.IsChecked;
            var untagged = UntaggedBox.IsChecked != null && (bool) UntaggedBox.IsChecked;
            var customClause = CustomClauseBox.Text;

            var command = new SQLiteCommand(connection);

            var nameParameter = new SQLiteParameter();
            command.Parameters.Add(nameParameter);
            nameParameter.Value = name;

            var ascendingParameter = new SQLiteParameter();
            command.Parameters.Add(ascendingParameter);
            ascendingParameter.Value = ascending;

            var searchTagsParameter = new SQLiteParameter();
            command.Parameters.Add(searchTagsParameter);
            searchTagsParameter.Value = searchTags;

            var searchFileNameParameter = new SQLiteParameter();
            command.Parameters.Add(searchFileNameParameter);
            searchFileNameParameter.Value = searchFileName;

            var untaggedParameter = new SQLiteParameter();
            command.Parameters.Add(untaggedParameter);
            untaggedParameter.Value = untagged;

            var customClauseParameter = new SQLiteParameter();
            command.Parameters.Add(customClauseParameter);
            customClauseParameter.Value = customClause;

            if (_listId < 0)
            {
                command.CommandText = "INSERT INTO viewLists (name, listIndex, orderBy, ascending, rating, searchString, searchTags, searchFileName, untagged, customClause) VALUES " + "(?, " + _listIndex + ", '" + orderBy + "', ?, " + rating + ", '" + searchString + "', ?, ?, ?, ?)";
            }
            else
            {
                command.CommandText = "UPDATE viewLists SET name = ?, orderBy = '" + orderBy + "', ascending = ?, rating = " + rating + ", searchString = '" + searchString + "', searchTags = ?, searchFileName = ?, untagged = ?, customClause = ? WHERE id = " + _listId;
            }

            command.ExecuteNonQuery();
            DialogResult = true;
        }

        private void tagsSearchBox_Click(object sender, RoutedEventArgs e)
        {
            if (_searchBox == null) return;
            if (TagsSearchBox.IsChecked != null) _searchBox.AutoCompleteEnabled = (bool) TagsSearchBox.IsChecked;
        }
    }
}