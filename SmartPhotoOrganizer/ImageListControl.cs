using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer
{
    public static class ImageListControl
    {
        private static DispatcherTimer _timer;
        private static int _imageListVersion;
        private static bool _clearCacheWhenLoadCompletes;

        static ImageListControl()
        {
            ImageList = new List<int>();
        }

        public static int CurrentIndex { get; private set; } = -1;
        public static bool Playing { get; private set; }
        public static List<int> ImageList { get; private set; }
        public static int CurrentImageId => ImageList[CurrentIndex];

        public static ImageUserData CurrentImageData
            => Database.GetImageUserData(PhotoManager.Connection, CurrentImageId);

        public static int TotalImages => ImageList.Count;

        public static void MoveLeft()
        {
            MoveLeft(true);
        }

        private static void MoveLeft(bool userInitiated)
        {
            if (userInitiated)
            {
                StopSlideshowIfRunning();
            }

            if (PhotoManager.ImageShowing && CurrentIndex > 0)
            {
                CurrentIndex--;

                PhotoManager.MainWindow.RemoveFromCache(ImageCache.Get(ImageCache.NumImagesToCacheAhead).UiImage);
                ImageCache.ShiftRight();

                // If we have an image cached, display it.
                if (ImageCache.Current.Image != null)
                {
                    PhotoManager.MainWindow.SetPicture(ImageCache.Current);

                    // Load more images if we need to.
                    StartLoadingAdditionalImages();
                }
                else
                {
                    // If we don't, start loading the image, if it's not loading already.
                    PhotoManager.MainWindow.SetLoadingPicture();

                    if (!ImageCache.Current.CallInProgress)
                    {
                        LoadImage(CurrentIndex);
                    }
                }
            }
        }

        public static void MoveRight()
        {
            MoveRight(true);
        }

        private static void MoveRight(bool userInitiated)
        {
            if (userInitiated)
            {
                StopSlideshowIfRunning();
            }

            if (PhotoManager.ImageShowing && CurrentIndex < ImageList.Count - 1)
            {
                CurrentIndex++;

                PhotoManager.MainWindow.RemoveFromCache(ImageCache.Get(-ImageCache.NumImagesToCacheAhead).UiImage);
                ImageCache.ShiftLeft();

                // If we have an image cached, display it.
                if (ImageCache.Current.Image != null)
                {
                    PhotoManager.MainWindow.SetPicture(ImageCache.Current);

                    // Load more images if we need to.
                    StartLoadingAdditionalImages();
                }
                else
                {
                    // If we don't, start loading the image, if it's not loading already.
                    PhotoManager.MainWindow.SetLoadingPicture();

                    if (!ImageCache.Current.CallInProgress)
                    {
                        LoadImage(CurrentIndex);
                    }
                }
            }
        }

        public static void MoveToStart()
        {
            SetCurrentIndex(0);
        }

        public static void MoveToEnd()
        {
            SetCurrentIndex(ImageList.Count - 1);
        }

        public static void ToggleSlideshow()
        {
            if (!Playing)
            {
                StartSlideshow();
            }
            else
            {
                StopSlideshow();
            }
            PhotoManager.MainWindow.UpdateInfoBar();
        }

        public static bool StopSlideshowIfRunning()
        {
            if (Playing)
            {
                StopSlideshow();
                PhotoManager.MainWindow.UpdateInfoBar();
                return true;
            }
            return false;
        }

        private static void StartSlideshow()
        {
            _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(PhotoManager.Config.SlideshowDelaySeconds)};
            _timer.Tick += AdvanceSlideshow;
            _timer.Start();
            Playing = true;
        }

        private static void AdvanceSlideshow(object sender, EventArgs e)
        {
            if (ImageList.Count == 0)
            {
                return;
            }

            if (CurrentIndex == ImageList.Count - 1)
            {
                StopSlideshow();
                MoveToStart();
                StartSlideshow();
            }
            else
            {
                MoveRight(false);
            }
        }

        private static void StopSlideshow()
        {
            _timer.Stop();
            _timer = null;
            Playing = false;
        }

        /// <summary>
        /// Runs the current image query, and if we have a new image list, make it current and change
        /// the view to the given index.
        /// </summary>
        /// <param name="startingIndex">The index to start at if we get a new image list.</param>
        public static void RunImageQuery(int startingIndex)
        {
            StopSlideshowIfRunning();
            var newImageList = ImageQuery.GetImageList(PhotoManager.Connection);
            if (newImageList == null) return;
            ImageList = newImageList;
            SetCurrentIndex(startingIndex);
        }

        public static void RunImageQueryForced()
        {
            ImageList = ImageQuery.GetImageList(PhotoManager.Connection, true);
            SetCurrentIndex(0);
        }

        public static void RefreshImageCache()
        {
            SetCurrentIndex(CurrentIndex);
        }

        public static void DeleteCurrentImage()
        {
            StopSlideshowIfRunning();

            if (ImageCache.Current.Image != null)
            {
                if (MessageBox.Show("Really delete?", "Confirm delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var imageId = CurrentImageId;
                        var imageName = Database.GetImageName(PhotoManager.Connection, imageId);

                        // Delete file
                        File.Delete(imageName);

                        var indexChanged = false;

                        // Remove image from internal structures
                        Database.RemoveImageData(imageId, PhotoManager.Connection);

                        ImageList.RemoveAt(CurrentIndex);

                        if (CurrentIndex == ImageList.Count)
                        {
                            CurrentIndex--;
                            indexChanged = true;
                        }

                        // Remove cached UI image.
                        PhotoManager.MainWindow.RemoveFromCache(ImageCache.Current.UiImage);

                        // Remove cached image data. If the index changed, we signal that this is the last item.
                        ImageCache.DeleteCurrent(indexChanged);

                        // Increment list version so in-progress calls are ignored when they complete.
                        _imageListVersion++;

                        // Show our new cached image.
                        PhotoManager.MainWindow.SetPicture(ImageCache.Current);

                        // Start loading new images if we need to.
                        if (ImageList.Count > 0)
                        {
                            StartLoadingAdditionalImages();
                        }
                    }
                    catch (IOException)
                    {
                        HandleDeleteException();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        HandleDeleteException();
                    }
                }
            }
        }

        /// <summary>
        /// Called when files have been deleted and we need to update our data structures.
        /// </summary>
        /// <param name="deletedImages">The images that have been deleted.</param>
        public static void RemoveDeletedImageData(List<int> deletedImages)
        {
            if (deletedImages.Count > 0)
            {
                foreach (var imageId in deletedImages)
                {
                    var indexOfImage = ImageList.IndexOf(imageId);

                    if (indexOfImage < 0) continue;
                    ImageList.RemoveAt(indexOfImage);

                    if (indexOfImage < CurrentIndex)
                    {
                        CurrentIndex--;
                    }
                }

                Database.RemoveImageData(deletedImages, PhotoManager.Connection);
                ImageCache.Current.Image = null;

                if (CurrentIndex == ImageList.Count)
                {
                    CurrentIndex--;
                }
                SetCurrentIndex(CurrentIndex);
            }
            SetCurrentIndex(CurrentIndex);
        }

    private static void SetCurrentIndex(int index)
        {
            StopSlideshowIfRunning();

            if (index >= 0 && index < ImageList.Count)
            {
                // Set the current index.
                CurrentIndex = index;

                // Clear the UI cache when we're done doing the load.
                _clearCacheWhenLoadCompletes = true;

                // Clear the image cache now.
                ImageCache.Clear();

                // Increment the image list version, so pending loads are thrown away.
                _imageListVersion++;

                // Begin loading the image at the current index.
                LoadImage(CurrentIndex);
            }
            else
            {
                if (ImageQuery.HasFilter)
                {
                    PhotoManager.MainWindow.ShowNoResults();
                }
                else
                {
                    PhotoManager.MainWindow.ShowNoImages();
                }
            }
        }

        private static void LoadImage(int index)
        {
            var imageLoader = new BackgroundWorker();
            var aspectRatio = PhotoManager.MainWindow.ImageAreaAspectRatio;
            var imageId = ImageList[index];
            var startingImageListVersion = _imageListVersion;

            ImageCache.Get(index - CurrentIndex).CallInProgress = true;

            imageLoader.DoWork += delegate (object sender, DoWorkEventArgs args)
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                var workerConnection = new SQLiteConnection(Database.ConnectionString);
                workerConnection.Open();

                args.Result = new ImageViewer(imageId, aspectRatio, workerConnection);
            };

            imageLoader.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs args)
            {
                // We've since thrown away the image list we started on, throw it away.
                if (startingImageListVersion != _imageListVersion)
                {
                    return;
                }

                if (_clearCacheWhenLoadCompletes)
                {
                    PhotoManager.MainWindow.ClearCache();

                    _clearCacheWhenLoadCompletes = false;
                }

                var indexOffset = index - CurrentIndex;

                // The image we've got isn't inside the cache window, throw it away.
                if (Math.Abs(indexOffset) > ImageCache.NumImagesToCacheAhead)
                {
                    return;
                }

                // We already have an image in cache for this index, throw it away.
                if (ImageCache.Get(indexOffset).Image != null)
                {
                    return;
                }

                var finishedImage = args.Result as ImageViewer;

                // Populate the image cache with the built ViewerImage.
                ImageCache.Get(indexOffset).Image = finishedImage;
                ImageCache.Get(indexOffset).UiImage = PhotoManager.MainWindow.AddToCache(finishedImage);
                if (finishedImage != null && finishedImage.Rotation != 0)
                {
                    ImageCache.Get(indexOffset).UiImage.LayoutTransform = new RotateTransform(finishedImage.Rotation);
                }

                // If we came back with the current image, display it.
                if (indexOffset == 0)
                {
                    PhotoManager.MainWindow.SetPicture(ImageCache.Get(indexOffset));
                }

                // Start loading more images in the background if needed.
                StartLoadingAdditionalImages();
            };

            imageLoader.RunWorkerAsync();
        }

        private static void StartLoadingAdditionalImages()
        {
            if (ImageCache.Current.Image == null && !ImageCache.Current.CallInProgress)
            {
                LoadImage(CurrentIndex);
            }

            var leftBoundIndex = Math.Max(-ImageCache.NumImagesToCacheAhead, -CurrentIndex);
            var rightBoundIndex = Math.Min(ImageCache.NumImagesToCacheAhead, ImageList.Count - CurrentIndex - 1);

            var furthestLoaded = -1;
            var furthestPossible = 0;

            if (ImageCache.Current.Image != null)
            {
                furthestLoaded = 0;
            }

            for (var i = 1; i <= ImageCache.NumImagesToCacheAhead; i++)
            {
                var rightIndexInBounds = i <= rightBoundIndex;
                var leftIndexInBounds = -i >= leftBoundIndex;

                if (rightIndexInBounds || leftIndexInBounds)
                {
                    furthestPossible = i;
                }
                else
                {
                    break;
                }

                var rightImage = ImageCache.Get(i).Image != null;
                var leftImage = ImageCache.Get(-i).Image != null;

                if ((rightImage || !rightIndexInBounds) && (leftImage || !leftIndexInBounds))
                {
                    if (furthestLoaded == i - 1)
                    {
                        furthestLoaded++;
                    }
                }
            }

            if (furthestLoaded < furthestPossible && furthestLoaded >= 0)
            {
                var indexOffsetToLoad = furthestLoaded + 1;

                if (-indexOffsetToLoad >= leftBoundIndex &&
                    !ImageCache.Get(-indexOffsetToLoad).CallInProgress)
                {
                    LoadImage(CurrentIndex - indexOffsetToLoad);
                }

                if (indexOffsetToLoad <= rightBoundIndex &&
                    !ImageCache.Get(indexOffsetToLoad).CallInProgress)
                {
                    LoadImage(CurrentIndex + indexOffsetToLoad);
                }
            }
        }

        private static void HandleDeleteException()
        {
            MessageBox.Show("Could not delete file.  Make sure that it is not write-protected or in use.", "Error with file delete");
        }
    }
}
