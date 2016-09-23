using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for ViewLists.xaml
    /// </summary>
    public partial class ViewLists
    {
        public ViewLists()
        {
            InitializeComponent();
            PhotoManager.Config.ApplyWindowSize("ViewLists", this);
            PopulateListBox();
            ViewListsBox.SelectionChanged += viewListsBox_SelectionChanged;
        }

        private void viewListsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemSelected = ViewListsBox.SelectedIndex >= 0;

            EditListButton.IsEnabled = itemSelected;
            ShowListButton.IsEnabled = itemSelected;
            DeleteListButton.IsEnabled = itemSelected;
            SetAsStartListButton.IsEnabled = itemSelected;
            UpButton.IsEnabled = itemSelected;
            DownButton.IsEnabled = itemSelected;

            if (!itemSelected) return;

            if (ViewListsBox.SelectedIndex == 0)
            {
                UpButton.IsEnabled = false;
            }
            else if (ViewListsBox.SelectedIndex == ViewListsBox.Items.Count - 1)
            {
                DownButton.IsEnabled = false;
            }

            var selectedListId = (int)((ListBoxItem)ViewListsBox.SelectedItem).Tag;

            if (selectedListId != PhotoManager.Config.StartingListId) return;

            DeleteListButton.IsEnabled = false;
            SetAsStartListButton.IsEnabled = false;
        }

        private void lbi_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowSelectedList();
            DialogResult = true;
        }

        private void PopulateListBox()
        {
            ViewListsBox.Items.Clear();
            var command = new SQLiteCommand("SELECT id, name FROM viewLists ORDER BY listIndex", PhotoManager.Connection);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var listId = reader.GetInt32("id");
                    var name = reader.GetString("name");
                    var listBoxItem = new ListBoxItem();

                    if (listId == PhotoManager.Config.StartingListId)
                    {
                        var viewListPanel = new StackPanel {Orientation = Orientation.Horizontal};
                        var viewListNameBlock = new TextBlock { Text = name };
                        viewListPanel.Children.Add(viewListNameBlock);
                        var startingIdentifierBlock = new TextBlock();
                        var italicText = new Run { FontStyle = FontStyles.Italic, FontWeight = FontWeights.Bold, Text = "(Starting List)" };
                        startingIdentifierBlock.Inlines.Add(italicText);
                        startingIdentifierBlock.Margin = new Thickness(6, 0, 0, 0);
                        viewListPanel.Children.Add(startingIdentifierBlock);
                        listBoxItem.Content = viewListPanel;
                    }
                    else
                    {
                        listBoxItem.Content = name;
                    }

                    listBoxItem.Tag = listId;
                    listBoxItem.MouseDoubleClick += lbi_MouseDoubleClick;

                    var menu = new ContextMenu();
                    var editItem = new MenuItem {Header = "Edit"};
                    editItem.Click += EditList;
                    menu.Items.Add(editItem);

                    if (listId != PhotoManager.Config.StartingListId)
                    {
                        var deleteItem = new MenuItem {Header = "Delete"};
                        deleteItem.Click += DeleteList;
                        menu.Items.Add(deleteItem);
                        var setAsStartListItem = new MenuItem {Header = "Set as Start List"};
                        setAsStartListItem.Click += SetAsStartList;
                        menu.Items.Add(setAsStartListItem);
                    }
                    listBoxItem.ContextMenu = menu;
                    ViewListsBox.Items.Add(listBoxItem);
                }
            }
        }

        private void newListButton_Click(object sender, RoutedEventArgs e)
        {
            var editViewList = new EditViewList(-1, ViewListsBox.Items.Count) {Owner = this};
            editViewList.ShowDialog();

            var selectedIndex = ViewListsBox.SelectedIndex;
            PopulateListBox();
            ViewListsBox.SelectedIndex = selectedIndex;
        }

        private void EditList(object sender, RoutedEventArgs e)
        {
            if (ViewListsBox.SelectedIndex < 0) return;
            var editViewList = new EditViewList((int) ((ListBoxItem) ViewListsBox.SelectedItem).Tag, -1)
            {
                Owner = this
            };
            editViewList.ShowDialog();
            var selectedIndex = ViewListsBox.SelectedIndex;
            PopulateListBox();
            ViewListsBox.SelectedIndex = selectedIndex;
        }

        private void DeleteList(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Really delete the list?", "Confirm delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var idToDelete = (int)((ListBoxItem)ViewListsBox.SelectedItem).Tag;

                using (var transaction = PhotoManager.Connection.BeginTransaction())
                {
                    Database.ExecuteNonQuery("DELETE FROM viewLists WHERE id = " + idToDelete, PhotoManager.Connection);

                    RefreshListIndices();

                    transaction.Commit();
                }

                PopulateListBox();
            }
        }

        private void SetAsStartList(object sender, RoutedEventArgs e)
        {
            PhotoManager.Config.StartingListId = (int)((ListBoxItem)ViewListsBox.SelectedItem).Tag;

            var selectedIndex = ViewListsBox.SelectedIndex;
            PopulateListBox();
            ViewListsBox.SelectedIndex = selectedIndex;
        }

        private void showListButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSelectedList();
            DialogResult = true;
        }

        private void ShowSelectedList()
        {
            var idToDisplay = (int)((ListBoxItem)ViewListsBox.SelectedItem).Tag;

            QueryOperations.ShowImageList(idToDisplay);
        }

        private void RefreshListIndices()
        {
            for (var i = 0; i < ViewListsBox.Items.Count; i++)
            {
                var item = (ListBoxItem)ViewListsBox.Items[i];
                var itemId = (int)item.Tag;

                Database.ExecuteNonQuery("UPDATE viewLists SET listIndex = " + i + " WHERE id = " + itemId, PhotoManager.Connection);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PhotoManager.Config.SaveWindowSize("ViewLists", this);
        }

        private void upButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = ViewListsBox.SelectedIndex;

            var itemToMove = (ListBoxItem)ViewListsBox.SelectedItem;
            ViewListsBox.Items.Remove(itemToMove);

            ViewListsBox.Items.Insert(selectedIndex - 1, itemToMove);

            using (var transaction = PhotoManager.Connection.BeginTransaction())
            {
                RefreshListIndices();

                transaction.Commit();
            }

            ViewListsBox.SelectedIndex = selectedIndex - 1;
        }

        private void downButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = ViewListsBox.SelectedIndex;

            var itemToMove = (ListBoxItem)ViewListsBox.SelectedItem;
            ViewListsBox.Items.Remove(itemToMove);

            ViewListsBox.Items.Insert(selectedIndex + 1, itemToMove);

            using (var transaction = PhotoManager.Connection.BeginTransaction())
            {
                RefreshListIndices();

                transaction.Commit();
            }

            ViewListsBox.SelectedIndex = selectedIndex + 1;
        }
    }
}