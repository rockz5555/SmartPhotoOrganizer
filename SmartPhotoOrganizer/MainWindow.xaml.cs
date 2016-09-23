using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SmartPhotoOrganizer.DatabaseOp;
using SmartPhotoOrganizer.DataStructures;
using SmartPhotoOrganizer.InputRelated;
using SmartPhotoOrganizer.UIAspects;

namespace SmartPhotoOrganizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public bool AllowInput { get; set; } = true;
        private bool _overlayInfobar = true;
        private InputMapper _inputMapper;
        private ImageViewer _currentImage;

        public MainWindow()
        {
            InitializeComponent();
            PhotoManager.Initiate(this);
            _inputMapper = new InputMapper(PhotoManager.Connection);
            var fullscreenStart = PhotoManager.Config.FullscreenStartSetting;

            switch (fullscreenStart)
            {
                case FullscreenStart.RememberLast:
                    if (PhotoManager.Config.FullscreenStart)
                    {
                        SetFullscreen();
                    }
                    else
                    {
                        SetWindowed();
                    }
                    break;
                case FullscreenStart.AlwaysFullscreen:
                    SetFullscreen();
                    break;
                case FullscreenStart.AlwaysWindowed:
                    SetWindowed();
                    break;
            }
            if (!PhotoManager.Config.OverlayInfobar)
            {
                OverlayInfobar = false;
            }
        }

        public void SetPicture(CachedImage image)
        {
            InformationalText.Visibility = Visibility.Collapsed;
            ErrorText.Visibility = Visibility.Collapsed;

            if (image?.Image != null)
            {
                var imageViewer = image.Image;

                if (imageViewer.IsAnimated)
                {
                    try
                    {
                        GifImage.GifSource = Database.GetImageName(PhotoManager.Connection, imageViewer.ImageId);
                    }
                    catch (FileNotFoundException)
                    {
                        imageViewer.FileExists = false;
                    }
                    BackgroundRectangle.Visibility = Visibility.Hidden;
                    ImageGrid.Visibility = Visibility.Hidden;
                    GifImage.Visibility = Visibility.Visible;
                }
                else
                {
                    GifImage.GifSource = null;
                    GifImage.Visibility = Visibility.Hidden;
                    BackgroundRectangle.Visibility = Visibility.Visible;
                    ImageGrid.Visibility = Visibility.Visible;

                    ImageGrid.Children.Remove(image.UiImage);
                    ImageGrid.Children.Remove(BackgroundRectangle);
                    ImageGrid.Children.Add(BackgroundRectangle);

                    if (!imageViewer.Corrupted && imageViewer.FileExists)
                    {
                        ImageGrid.Children.Add(image.UiImage);
                        BackgroundRectangle.Fill = imageViewer.BgColor;
                    }
                    else
                    {
                        BackgroundRectangle.Fill = Brushes.Black;
                    }
                }
                if (!imageViewer.FileExists)
                {
                    ErrorText.Text = "File no longer exists.";
                    ErrorText.Visibility = Visibility.Visible;
                }
                if (imageViewer.Corrupted)
                {
                    ErrorText.Text = "Could not read image.";
                    ErrorText.Visibility = Visibility.Visible;
                }
                _currentImage = imageViewer;
            }
            else
            {
                GifImage.Visibility = Visibility.Hidden;
                BackgroundRectangle.Visibility = Visibility.Hidden;
                ImageGrid.Visibility = Visibility.Hidden;
                GifImage.GifSource = null;
                _currentImage = null;
            }
            UpdateInfoBar();
        }

        public void ShowNoResults()
        {
            GifImage.Visibility = Visibility.Hidden;
            BackgroundRectangle.Visibility = Visibility.Hidden;
            ImageGrid.Visibility = Visibility.Hidden;
            ErrorText.Visibility = Visibility.Collapsed;
            InformationalText.Text = "No image met filter critera.";
            InformationalText.Visibility = Visibility.Visible;
            GifImage.GifSource = null;
            _currentImage = null;
            UpdateInfoBar();
        }

        public void ShowNoImages()
        {
            GifImage.Visibility = Visibility.Hidden;
            BackgroundRectangle.Visibility = Visibility.Hidden;
            ImageGrid.Visibility = Visibility.Hidden;
            ErrorText.Visibility = Visibility.Collapsed;
            InformationalText.Text = "No images present in library.";
            InformationalText.Visibility = Visibility.Visible;
            GifImage.GifSource = null;
            _currentImage = null;
            UpdateInfoBar();
        }

        public void SetLoadingPicture()
        {
            ImageGrid.Children.Remove(BackgroundRectangle);
            ImageGrid.Children.Add(BackgroundRectangle);
            InformationalText.Text = "Loading...";
            InformationalText.Visibility = Visibility.Visible;
            BackgroundRectangle.Fill = Brushes.Black;
            UpdateInfoBarUserData(ImageListControl.CurrentImageData);
        }

        public Image AddToCache(ImageViewer imageViewer)
        {
            if (!imageViewer.FileExists || imageViewer.IsAnimated || imageViewer.Corrupted)
            {
                return null;
            }

            var image = imageViewer.Image;
            var uiImage = new Image { Source = image };

            // Sometimes the DPI goes crazy and we need to correct it. This is a workaround, these images will only be
            // displayed correctly initially.
            if (image.DpiX < 2.0 && image.DpiY > 10.0 || image.DpiY < 2.0 && image.DpiX > 10.0)
            {
                var holderAspectRatio = ImageAreaAspectRatio;
                double imageAspectRatio;
                if (imageViewer.Rotation == 0 || imageViewer.Rotation == 180)
                {
                    imageAspectRatio = ((double)image.PixelWidth) / image.PixelHeight;
                }
                else
                {
                    imageAspectRatio = ((double)image.PixelHeight) / image.PixelWidth;
                }
                uiImage.Stretch = Stretch.Fill;
                if (imageAspectRatio > holderAspectRatio)
                {
                    uiImage.Width = ImageGrid.ActualWidth;
                    uiImage.Height = ImageGrid.ActualWidth / imageAspectRatio;
                }
                else
                {
                    uiImage.Width = ImageGrid.ActualHeight * imageAspectRatio;
                    uiImage.Height = ImageGrid.ActualHeight;
                }
            }
            ImageGrid.Children.Insert(0, uiImage);
            return uiImage;
        }

        public void RemoveFromCache(Image uiImage)
        {
            if (uiImage != null)
            {
                ImageGrid.Children.Remove(uiImage);
            }
        }

        public void ClearCache()
        {
            ImageGrid.Children.RemoveRange(0, ImageGrid.Children.Count);
        }

        public void UpdateInfoBar()
        {
            InfoBarGrid.Visibility = Visibility.Visible;

            if (PhotoManager.ImageShowing && _currentImage != null)
            {
                var imageData = ImageListControl.CurrentImageData;

                if (imageData.Tags.Count > 0)
                {
                    TagsPanel.Visibility = Visibility.Visible;
                    TagsText.Text = imageData.TagsString;
                }
                else
                {
                    TagsPanel.Visibility = Visibility.Collapsed;
                }

                FileName.Text = PhotoManager.Config.SourceDirectories.GetRelativeName(imageData.Name);

                if (imageData.Rating > 0)
                {
                    Rating.Visibility = Visibility.Visible;
                    Rating.Text = imageData.Rating.ToString();

                    Rating.Foreground = Utilities.GetColorFromRating(imageData.Rating);
                }
                else
                {
                    Rating.Visibility = Visibility.Collapsed;
                }
                PlayIcon.Visibility = ImageListControl.Playing ? Visibility.Visible : Visibility.Collapsed;
                ViewIndex.Text = (ImageListControl.CurrentIndex + 1) + "/" + ImageListControl.TotalImages;
            }
            else
            {
                TagsPanel.Visibility = Visibility.Collapsed;
                Duplicate.Visibility = Visibility.Collapsed;
                Rating.Visibility = Visibility.Collapsed;
                PlayIcon.Visibility = Visibility.Collapsed;

                FileName.Text = "";

                ViewIndex.Text = "0/0";
            }
            UpdateImageQueryInfo();
        }

        private void UpdateInfoBarUserData(ImageUserData imageData)
        {
            if (imageData.Tags.Count > 0)
            {
                TagsPanel.Visibility = Visibility.Visible;
                TagsText.Text = imageData.TagsString;
            }
            else
            {
                TagsPanel.Visibility = Visibility.Collapsed;
            }

            FileName.Text = PhotoManager.Config.SourceDirectories.GetRelativeName(imageData.Name);
            Duplicate.Visibility = Visibility.Collapsed;

            if (imageData.Rating > 0)
            {
                Rating.Visibility = Visibility.Visible;
                Rating.Text = imageData.Rating.ToString();

                Rating.Foreground = Utilities.GetColorFromRating(imageData.Rating);
            }
            else
            {
                Rating.Visibility = Visibility.Collapsed;
            }
            UpdateImageQueryInfo();
        }

        private void UpdateImageQueryInfo()
        {
            if (ImageQuery.ListName != "" && QueryOperations.CurrentListId != PhotoManager.Config.StartingListId)
            {
                Order.Text = "List: " + ImageQuery.ListName;
            }
            else
            {
                if (ImageQuery.Sort == SortType.Modified)
                {
                    Order.Text = "Last Modified";

                    if (ImageQuery.Ascending)
                    {
                        Order.Text += ": Oldest First";
                    }
                }
                else if (ImageQuery.Sort == SortType.Random)
                {
                    Order.Text = "Random";
                }
                else if (ImageQuery.Sort == SortType.Name)
                {
                    Order.Text = "File Name";

                    if (!ImageQuery.Ascending)
                    {
                        Order.Text += ": Reverse Order";
                    }
                }

                var filters = new List<string>();
                var ratingView = ImageQuery.MinRating;

                if (ratingView == -1)
                {
                    filters.Add("Unrated");
                }
                else if (ratingView > 0)
                {
                    filters.Add(ratingView.ToString() + "+");
                }
                else
                {
                    FiltersText.Text = "";
                }
                if (ImageQuery.UntaggedOnly)
                {
                    filters.Add("Untagged");
                }
                if (ImageQuery.Search != "")
                {
                    filters.Add("\"" + ImageQuery.Search + "\"");
                }
                if (ImageQuery.CustomClause != "")
                {
                    filters.Add("Custom Clause");
                }
                if (filters.Count > 0)
                {
                    FiltersPanel.Visibility = Visibility.Visible;
                    FiltersText.Text = string.Join(", ", filters.ToArray());
                }
                else
                {
                    FiltersPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        public bool ProgressBarVisible
        {
            get
            {
                return IndexingProgressPanel.Visibility == Visibility.Visible;
            }
            set
            {
                if (value)
                {
                    GifImage.Visibility = Visibility.Hidden;
                    InfoBarGrid.Visibility = Visibility.Collapsed;
                    IndexProgress.Value = 0;
                    IndexingProgressPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    GifImage.Visibility = Visibility.Visible;
                    IndexingProgressPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void ReportProgress(double progress)
        {
            IndexProgress.Value = progress;
        }

        public bool OverlayInfobar
        {
            set
            {
                if (_overlayInfobar != value)
                {
                    _overlayInfobar = value;

                    if (value)
                    {
                        Grid.SetRowSpan(ImageGrid, 2);
                        Grid.SetRowSpan(GifImage, 2);
                        OverlayBorder.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Grid.SetRowSpan(ImageGrid, 1);
                        Grid.SetRowSpan(GifImage, 1);
                        OverlayBorder.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        public bool FullScreen { get; private set; } = true;

        public double ImageAreaAspectRatio => ImageGrid.ActualWidth / ImageGrid.ActualHeight;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            var alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

            if (control)
            {
                ExecuteUserAction(_inputMapper.GetActionFromKeyCtrl(e.Key));
            }
            else if (alt)
            {
                ExecuteUserAction(_inputMapper.GetActionFromKeyAlt(e.SystemKey));
            }
            else
            {
                ExecuteUserAction(_inputMapper.GetActionFromKey(e.Key));
            }
        }

        private void ExecuteUserAction(UserAction action)
        {
            if (!AllowInput && action != UserAction.ToggleFullscreen && action != UserAction.Quit)
            {
                return;
            }

            switch (action)
            {
                case UserAction.None:
                    break;
                case UserAction.ShowHelp:
                    var editKeys = new EditKeys { Owner = this };
                    if (editKeys.ShowDialog() == true)
                    {
                        _inputMapper = new InputMapper(PhotoManager.Connection);
                    }
                    break;
                case UserAction.ShowOptions:
                    var settingsWindow = new SettingsWindow(this);
                    if (settingsWindow.ShowDialog() == true)
                    {
                        PhotoManager.UpdateSettings(settingsWindow);
                    }
                    break;
                case UserAction.ReloadFilesFromDisk:
                    PhotoManager.ReloadFiles();
                    break;
                case UserAction.ToggleFullscreen:
                    ToggleFullscreen();
                    break;
                case UserAction.Minimize:
                    WindowState = WindowState.Minimized;
                    break;
                case UserAction.Quit:
                    Close();
                    break;
                case UserAction.RateAs1:
                    EditOperations.RateImage(1);
                    break;
                case UserAction.RateAs2:
                    EditOperations.RateImage(2);
                    break;
                case UserAction.RateAs3:
                    EditOperations.RateImage(3);
                    break;
                case UserAction.RateAs4:
                    EditOperations.RateImage(4);
                    break;
                case UserAction.RateAs5:
                    EditOperations.RateImage(5);
                    break;
                case UserAction.ClearRating:
                    EditOperations.RateImage(0);
                    break;
                case UserAction.Tag:
                    if (PhotoManager.ImageShowing)
                    {
                        ImageListControl.StopSlideshowIfRunning();
                        var tagBox = new EditTagBox(this);
                        tagBox.ShowDialog();
                    }
                    break;
                case UserAction.TagEditMultiple:
                    if (PhotoManager.ImageShowing)
                    {
                        var editMultipleTagBox = new EditMultipleTags(this);
                        editMultipleTagBox.ShowDialog();
                    }
                    break;
                case UserAction.TagRenameOrDelete:
                    var batchTag = new TagRenameOrDelete { Owner = this };
                    batchTag.ShowDialog();
                    break;
                case UserAction.Rename:
                    ImageListControl.StopSlideshowIfRunning();
                    ShowRename();
                    break;
                case UserAction.Move:
                    EditOperations.MoveVisibleFiles();
                    Focus();
                    break;
                case UserAction.CopyFiles:
                    EditOperations.CopyVisibleFiles();
                    break;
                case UserAction.DeleteCurrentFile:
                    ImageListControl.DeleteCurrentImage();
                    break;
                case UserAction.ShowPreviousImage:
                    ImageListControl.MoveLeft();
                    break;
                case UserAction.ShowNextImage:
                    ImageListControl.MoveRight();
                    break;
                case UserAction.MoveToFirstImage:
                    ImageListControl.MoveToStart();
                    break;
                case UserAction.MoveToLastImage:
                    ImageListControl.MoveToEnd();
                    break;
                case UserAction.PlayStopSlideshow:
                    ImageListControl.ToggleSlideshow();
                    break;
                case UserAction.ShowLists:
                    var viewLists = new ViewLists { Owner = this };
                    viewLists.ShowDialog();
                    break;
                case UserAction.ChangeOrder:
                    QueryOperations.ChangeOrder();
                    break;
                case UserAction.Search:
                    ShowSearch();
                    break;
                case UserAction.ClearSearch:
                    QueryOperations.ClearSearch();
                    break;
                case UserAction.ShowRating1OrGreater:
                    QueryOperations.SetRatingView(1);
                    break;
                case UserAction.ShowRating2OrGreater:
                    QueryOperations.SetRatingView(2);
                    break;
                case UserAction.ShowRating3OrGreater:
                    QueryOperations.SetRatingView(3);
                    break;
                case UserAction.ShowRating4OrGreater:
                    QueryOperations.SetRatingView(4);
                    break;
                case UserAction.ShowRating5OrGreater:
                    QueryOperations.SetRatingView(5);
                    break;
                case UserAction.ClearRatingFilter:
                    QueryOperations.SetRatingView(0);
                    break;
                case UserAction.ShowOnlyUnrated:
                    QueryOperations.SetRatingView(-1);
                    break;
                case UserAction.ShowOnlyUntagged:
                    QueryOperations.ToggleUntagged();
                    break;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var clicks = e.Delta / -120;

            if (clicks > 0)
            {
                ExecuteUserAction(_inputMapper.GetActionFromMouseWheel(MouseWheelAction.Down));
            }
            else if (clicks < 0)
            {
                ExecuteUserAction(_inputMapper.GetActionFromMouseWheel(MouseWheelAction.Up));
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(this);
            var userAction = _inputMapper.GetActionFromMouseButton(e.ChangedButton);

            if (userAction == UserAction.None && e.ChangedButton == MouseButton.Left)
            {
                ExecuteUserAction(point.X < ActualWidth / 2 ? UserAction.ShowPreviousImage : UserAction.ShowNextImage);
            }
            else
            {
                ExecuteUserAction(userAction);
            }
        }

        private void ToggleFullscreen()
        {
            if (FullScreen)
            {
                SetWindowed();
            }
            else
            {
                PhotoManager.SaveWindowSize();
                SetFullscreen();
            }
        }

        private void SetFullscreen()
        {
            FullScreen = true;
            Cursor = Cursors.None;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }

        private void SetWindowed()
        {
            FullScreen = false;
            Cursor = Cursors.Arrow;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
        }

        private void ShowSearch()
        {
            var searchBox = new SearchBox(this);
            searchBox.ShowDialog();
        }

        private void ShowRename()
        {
            if (PhotoManager.ImageShowing)
            {
                var renameBox = new RenameBox(this);
                renameBox.ShowDialog();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                PhotoManager.SaveWindowSize();
            }
            if (PhotoManager.Config.FullscreenStartSetting == FullscreenStart.RememberLast)
            {
                PhotoManager.Config.FullscreenStart = FullScreen;
            }
        }
    }
}
