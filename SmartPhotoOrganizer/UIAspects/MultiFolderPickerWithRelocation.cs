using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace SmartPhotoOrganizer.UIAspects
{
    public class MultiFolderPickerWithRelocation : MultiFolderPicker
    {
        public List<string> MovedDirectories { get; } = new List<string>();

        protected override StackPanel CreateStackPanel(string text, bool? initialChecked, bool directoryExists)
        {
            var previousPanel = base.CreateStackPanel(text, initialChecked, directoryExists);
            if (directoryExists) return previousPanel;
            var specifyLocationButton = new Button { Content = "Specify New Location...", Margin = new Thickness(2) };
            specifyLocationButton.Click += specifyLocationButton_Click;
            previousPanel.Children.Add(specifyLocationButton);

            return previousPanel;
        }

        private void specifyLocationButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new VistaFolderBrowserDialog();
            if (folderDialog.ShowDialog() != true) return;
            var clickedButton = sender as Button;
            var stackPanel = clickedButton.Parent as StackPanel;
            var item = stackPanel.Parent as TreeViewItem;
            var path = item.Tag as string;

            AddFolder(folderDialog.SelectedPath);

            MovedDirectories.Add(path);

            var treeView = item.Parent as TreeView;
            treeView.Items.Remove(item);
        }
    }
}