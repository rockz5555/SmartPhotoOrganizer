using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for MultiFolderPicker.xaml
    /// </summary>
    public partial class MultiFolderPicker
    {
        public MultiFolderPicker()
        {
            InitializeComponent();
        }

        public DirectorySet DirectorySet
        {
            get
            {
                var result = new DirectorySet();

                foreach (TreeViewItem rootItem in FolderTreeView.Items)
                {
                    if (GetCheckBoxFromTreeItem(rootItem).IsChecked == false) continue;
                    var baseDir = new BaseDirectory {Path = rootItem.Tag as string};

                    AddExcludedDirectories(rootItem, baseDir.Exclusions);

                    result.BaseDirectories.Add(baseDir);
                }

                return result;
            }

            set
            {
                foreach (var baseDirectory in value.BaseDirectories)
                {
                    AddBaseDirectoryToTree(baseDirectory);
                }
            }
        }

        public bool HasDirectories
        {
            get
            {
                var hasDirectories = false;

                foreach (TreeViewItem rootItem in FolderTreeView.Items)
                {
                    if (GetCheckBoxFromTreeItem(rootItem).IsChecked != false)
                    {
                        hasDirectories = true;
                    }
                }

                return hasDirectories;
            }
        }

        public List<string> ExpandedDirectories
        {
            get
            {
                var result = new List<string>();

                foreach (TreeViewItem rootItem in FolderTreeView.Items)
                {
                    AddExpandedDirectories(rootItem, result);
                }

                return result;
            }

            set
            {
                foreach (TreeViewItem rootItem in FolderTreeView.Items)
                {
                    ExpandDirectories(rootItem, value);
                }
            }
        }

        private static void AddExpandedDirectories(TreeViewItem item, List<string> expandedDirectories)
        {
            if (item.Items.Count > 0 && item.IsExpanded)
            {
                expandedDirectories.Add(item.Tag as string);
            }

            foreach (TreeViewItem child in item.Items)
            {
                AddExpandedDirectories(child, expandedDirectories);
            }
        }

        private static void ExpandDirectories(TreeViewItem item, List<string> directoriesToExpand)
        {
            var currentPathLower = ((string) item.Tag).ToLowerInvariant();

            foreach (var directory in directoriesToExpand)
            {
                if (directory.ToLowerInvariant() == currentPathLower)
                {
                    item.IsExpanded = true;
                }
            }

            foreach (TreeViewItem child in item.Items)
            {
                ExpandDirectories(child, directoriesToExpand);
            }
        }

        private void AddBaseDirectoryToTree(BaseDirectory baseDirectory)
        {
            var directoryExists = Directory.Exists(baseDirectory.Path);

            var rootItem = new TreeViewItem
            {
                Header = CreateStackPanel(baseDirectory.Path, true, directoryExists),
                Tag = baseDirectory.Path
            };

            if (directoryExists)
            {
                AddSubdirectories(rootItem, baseDirectory, new DirectoryInfo(baseDirectory.Path), true);
            }

            FolderTreeView.Items.Add(rootItem);
        }

        private static void AddExcludedDirectories(TreeViewItem item, List<string> excludedDirectories)
        {
            foreach (TreeViewItem child in item.Items)
            {
                if (GetCheckBoxFromTreeItem(child).IsChecked == false)
                {
                    excludedDirectories.Add(child.Tag as string);
                }
                else
                {
                    AddExcludedDirectories(child, excludedDirectories);
                }
            }
        }

        private void AddSubdirectories(TreeViewItem parent, BaseDirectory baseDirectory, DirectoryInfo directory,
            bool currentDirectoryIncluded)
        {
            DirectoryInfo[] subdirectories;

            try
            {
                subdirectories = directory.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                // We ignore this directory.
                return;
            }

            var anyExcludedSubdirectories = false;

            foreach (var subdirectory in subdirectories)
            {
                var included = currentDirectoryIncluded &&
                                !baseDirectory.Exclusions.Contains(subdirectory.FullName.ToLowerInvariant());

                if (!included)
                {
                    anyExcludedSubdirectories = true;
                }

                var subItem = new TreeViewItem
                {
                    Header = CreateStackPanel(subdirectory.Name, included, true),
                    Tag = subdirectory.FullName.ToLowerInvariant()
                };
                parent.Items.Add(subItem);

                AddSubdirectories(subItem, baseDirectory, subdirectory, included);
            }

            if (currentDirectoryIncluded && anyExcludedSubdirectories)
            {
                MarkNodeAsIndeterminate(parent);
            }
        }

        private static void MarkNodeAsIndeterminate(TreeViewItem item)
        {
            var stackPanel = item.Header as StackPanel;
            var checkBox = stackPanel?.Children[0] as CheckBox;
            if (checkBox != null) checkBox.IsChecked = null;

            var parent = item.Parent as TreeViewItem;

            if (parent != null)
            {
                MarkNodeAsIndeterminate(parent);
            }
        }

        protected virtual StackPanel CreateStackPanel(string text, bool? initialChecked, bool directoryExists)
        {
            var treeItem = new StackPanel {Orientation = Orientation.Horizontal};
            var itemCheckbox = new CheckBox {Margin = new Thickness(2), IsChecked = initialChecked};
            itemCheckbox.Click += ItemCheckbox_Click;

            treeItem.Children.Add(itemCheckbox);

            var folderIcon = new Image
            {
                Margin = new Thickness(2),
                Source = new BitmapImage(new Uri("/UIAspects/folder_icon.png", UriKind.RelativeOrAbsolute))
            };

            treeItem.Children.Add(folderIcon);

            var textBlock = new TextBlock {Text = text, Margin = new Thickness(2)};
            treeItem.Children.Add(textBlock);

            if (!directoryExists)
            {
                textBlock.Foreground = System.Windows.Media.Brushes.Gray;

                var missingText = new TextBlock
                {
                    Text = "(missing)",
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(6, 2, 2, 2)
                };
                treeItem.Children.Add(missingText);
            }


            return treeItem;
        }

        private static void ItemCheckbox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;

            if (checkBox == null) return;

            var item = ((StackPanel) checkBox.Parent).Parent as TreeViewItem;

            if (checkBox.IsChecked != null && (bool) checkBox.IsChecked)
            {
                CheckChildren(item);
            }
            else
            {
                UncheckChildren(item);
            }

            if (item != null) RefreshCheckedState(item.Parent as TreeViewItem);
        }

        private static void UncheckChildren(TreeViewItem item)
        {
            foreach (TreeViewItem child in item.Items)
            {
                ((CheckBox) ((StackPanel) child.Header).Children[0]).IsChecked = false;

                UncheckChildren(child);
            }
        }

        private static void CheckChildren(TreeViewItem item)
        {
            foreach (TreeViewItem child in item.Items)
            {
                GetCheckBoxFromTreeItem(child).IsChecked = true;

                CheckChildren(child);
            }
        }

        private static void CheckTargetChild(TreeViewItem item, string targetChildPath)
        {
            foreach (TreeViewItem child in item.Items)
            {
                var childDir = child.Tag as string;

                if (childDir == targetChildPath)
                {
                    // This is the item we're looking for, check it.
                    GetCheckBoxFromTreeItem(child).IsChecked = true;

                    CheckChildren(child);
                    RefreshCheckedState(item);
                    ExpandNodeRecursive(item);

                }
                else if (childDir != null && (targetChildPath != null && targetChildPath.StartsWith(childDir)))
                {
                    // We're on the right track. Look in this node's children.
                    CheckTargetChild(child, targetChildPath);
                }
            }
        }

        private static void ExpandNodeRecursive(TreeViewItem item)
        {
            item.IsExpanded = true;

            var parent = item.Parent as TreeViewItem;

            if (parent != null)
            {
                ExpandNodeRecursive(parent);
            }
        }

        private static void RefreshCheckedState(TreeViewItem item)
        {
            if (item == null)
            {
                return;
            }

            var someChecked = false;
            var someUnchecked = false;

            foreach (TreeViewItem child in item.Items)
            {
                var childChecked = GetCheckBoxFromTreeItem(child).IsChecked;

                if (childChecked == null)
                {
                    someChecked = true;
                    someUnchecked = true;

                    break;
                }
                else if (childChecked == true)
                {
                    someChecked = true;

                    if (someUnchecked)
                    {
                        break;
                    }
                }
                else
                {
                    someUnchecked = true;

                    if (someChecked)
                    {
                        break;
                    }
                }
            }

            var checkBox = GetCheckBoxFromTreeItem(item);
            var previousChecked = checkBox.IsChecked;

            if (someUnchecked)
            {
                checkBox.IsChecked = null;
            }
            else if (someChecked)
            {
                checkBox.IsChecked = true;
            }

            if (checkBox.IsChecked != previousChecked)
            {
                RefreshCheckedState(item.Parent as TreeViewItem);
            }
        }

        private static void AddExclusions(BaseDirectory baseDirectory, TreeViewItem item)
        {
            foreach (TreeViewItem child in item.Items)
            {
                if (GetCheckBoxFromTreeItem(child).IsChecked == false)
                {
                    baseDirectory.Exclusions.Add(child.Tag as string);
                }
                else
                {
                    AddExclusions(baseDirectory, child);
                }
            }
        }

        private static CheckBox GetCheckBoxFromTreeItem(TreeViewItem item)
        {
            return (CheckBox) ((StackPanel) item.Header).Children[0];
        }

        private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = FolderTreeView.SelectedItem as TreeViewItem;

            if (selectedItem != null)
            {
                if (selectedItem.Parent is TreeViewItem)
                {
                    RemoveFolderButton.IsEnabled = false;
                    selectedItem.IsSelected = false;
                }
                else
                {
                    RemoveFolderButton.IsEnabled = true;
                }
            }
            else
            {
                RemoveFolderButton.IsEnabled = false;
            }
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new VistaFolderBrowserDialog();
            if (folderDialog.ShowDialog() == true)
            {
                AddFolder(folderDialog.SelectedPath);
            }
        }

        protected void AddFolder(string path)
        {
            var selectedPathLower = path.ToLowerInvariant();

            var childrenOfSelectedPath = new List<TreeViewItem>();

            foreach (TreeViewItem item in FolderTreeView.Items)
            {
                var basePath = item.Tag as string;

                if (basePath != null && basePath.ToLowerInvariant() == selectedPathLower)
                {
                    // The picked path matches a base directory already. Don't add anything.
                    return;
                }
                if (basePath != null && basePath.ToLowerInvariant().StartsWith(selectedPathLower))
                {
                    // The picked path is a parent of the base path. Combine all base directories that
                    //  fall under the picked path.
                    childrenOfSelectedPath.Add(item);
                }
                else if (basePath != null && selectedPathLower.StartsWith(basePath.ToLowerInvariant()))
                {
                    // The picked path is a child of the base path. Expand to that child and check it.
                    //  (if it exists in our snapshot)
                    CheckTargetChild(item, selectedPathLower);
                    return;
                }
            }

            var newBaseDirectory = new BaseDirectory {Path = path};

            foreach (var item in childrenOfSelectedPath)
            {
                if (GetCheckBoxFromTreeItem(item).IsChecked != false)
                {
                    AddExclusions(newBaseDirectory, item);
                }
                FolderTreeView.Items.Remove(item);
            }
            AddBaseDirectoryToTree(newBaseDirectory);
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FolderTreeView.SelectedItem as TreeViewItem;

            if (selectedItem != null) FolderTreeView.Items.Remove(selectedItem);
        }
    }
}