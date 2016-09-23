using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using SmartPhotoOrganizer.DataStructures;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow(MainWindow mainWindow)
        {
            Owner = mainWindow;
            InitializeComponent();

            PhotoManager.Config.ApplyWindowSize("Settings", this);

            var currentTab = (SettingsTab) PhotoManager.Config.GetConfigInt("LastSettingsTab");

            switch (currentTab)
            {
                case SettingsTab.General:
                    MainTabControl.SelectedItem = GeneralTab;
                    break;
                case SettingsTab.Library:
                    MainTabControl.SelectedItem = LibraryTab;
                    break;
            }

            OverlayInfobarBox.IsChecked = PhotoManager.Config.OverlayInfobar;
            SlideshowDelayBox.Text = PhotoManager.Config.SlideshowDelaySeconds.ToString();
            MultiFolderPicker.DirectorySet = PhotoManager.Config.SourceDirectories;
            MultiFolderPicker.ExpandedDirectories = PhotoManager.Config.ExpandedDirectories;

            var fullscreenStart = PhotoManager.Config.FullscreenStartSetting;

            switch (fullscreenStart)
            {
                case FullscreenStart.RememberLast:
                    FullscreenRemember.IsChecked = true;
                    break;
                case FullscreenStart.AlwaysFullscreen:
                    FullscreenFullscreen.IsChecked = true;
                    break;
                case FullscreenStart.AlwaysWindowed:
                    FullscreenWindowed.IsChecked = true;
                    break;
            }
        }

        public DirectorySet SourceDirectories => MultiFolderPicker.DirectorySet;

        public List<string> MovedDirectories => MultiFolderPicker.MovedDirectories;

        public string SlideshowDelaySeconds => SlideshowDelayBox.Text;

        public FullscreenStart FullscreenStart
        {
            get
            {
                if (FullscreenRemember.IsChecked == true)
                {
                    return FullscreenStart.RememberLast;
                }

                if (FullscreenFullscreen.IsChecked == true)
                {
                    return FullscreenStart.AlwaysFullscreen;
                }

                return FullscreenStart.AlwaysWindowed;
            }
        }

        public bool OverlayInfobar => OverlayInfobarBox.IsChecked != null && (bool) OverlayInfobarBox.IsChecked;

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (MultiFolderPicker.HasDirectories)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("You must pick at least one directory.");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            using (var transaction = PhotoManager.Connection.BeginTransaction())
            {
                PhotoManager.Config.SaveWindowSize("Settings", this);

                if (Equals(MainTabControl.SelectedItem, GeneralTab))
                {
                    PhotoManager.Config.SetConfigValue("LastSettingsTab", (int)SettingsTab.General);
                }
                else if (Equals(MainTabControl.SelectedItem, LibraryTab))
                {
                    PhotoManager.Config.SetConfigValue("LastSettingsTab", (int)SettingsTab.Library);
                }
                PhotoManager.Config.ExpandedDirectories = MultiFolderPicker.ExpandedDirectories;
                transaction.Commit();
            }
        }

        private void slideshowDelayBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (var c in e.Text)
            {
                if (!char.IsNumber(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void slideshowDelayBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void slideshowDelayBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => SlideshowDelayBox.SelectAll()));
        }
    }
}
