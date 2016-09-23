using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using SmartPhotoOrganizer.DatabaseOp;
using SmartPhotoOrganizer.DataStructures;
using SmartPhotoOrganizer.UIAspects;

namespace SmartPhotoOrganizer
{
    public static class PhotoManager
    {
        public static DbConfig Config { get; private set; }
        public static MainWindow MainWindow { get; private set; }

        private static readonly string[] Extensions = { "jpg", "jpeg", "gif", "png", "bmp", "tif" };
        private static List<string> _movedDirectoriesToProcess;

        public static void Initiate(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            var newDb = !File.Exists(Database.DbFile);

            if (newDb)
            {
                DirectorySet sourceDirectories = null;
                var helloWindow = new Hello();
                var showDialog = helloWindow.ShowDialog();

                if (showDialog != null && (bool) showDialog)
                {
                    sourceDirectories = helloWindow.SourceDirectories;
                }
                else
                {
                    Environment.Exit(0);
                }

                Connection = new SQLiteConnection(Database.ConnectionString);
                Connection.Open();

                Database.CreateTables(Connection);
                Database.PopulateDefaultConfig(Connection, sourceDirectories);
            }
            if (Connection == null)
            {
                Connection = new SQLiteConnection(Database.ConnectionString);
                Connection.Open();
            }
            Config = new DbConfig(Connection);
            Config.ApplyWindowSize("Main", MainWindow);
            QueryOperations.SetImageList(Config.StartingListId);
            RefreshFiles();
        }

        public static bool ImageShowing => ImageListControl.ImageList.Count > 0 && ImageCache.Current.Image != null;

        public static bool ImageLoading => ImageListControl.ImageList.Count > 0 && ImageCache.Current.CallInProgress;

        public static SQLiteConnection Connection { get; private set; }

        public static string CurrentImageName => Database.GetImageName(Connection, ImageListControl.CurrentImageId);

        public static bool SearchFileName => Config.SearchFileName;

        public static bool SearchTags => Config.SearchTags;

        public static List<string> CurrentTags => Database.GetTags(Connection, ImageListControl.CurrentImageId);

        public static void SaveWindowSize()
        {
            if (MainWindow.WindowState == WindowState.Normal)
            {
                Config.SaveWindowSize("Main", MainWindow);
            }
        }

        public static void UpdateSettings(SettingsWindow settingsWindow)
        {
            var sourceDirectories = settingsWindow.SourceDirectories;
            var movedDirectories = settingsWindow.MovedDirectories;
            var fullscreenStart = settingsWindow.FullscreenStart;
            var overlayInfobar = settingsWindow.OverlayInfobar;

            Config.FullscreenStartSetting = fullscreenStart;
            Config.OverlayInfobar = overlayInfobar;
            MainWindow.OverlayInfobar = overlayInfobar;

            int slideshowDelay;
            if (int.TryParse(settingsWindow.SlideshowDelaySeconds, out slideshowDelay) && slideshowDelay > 0)
            {
                Config.SlideshowDelaySeconds = slideshowDelay;
            }

            var refreshFiles = false;
            var refreshView = false;
            var refreshIncludedFlag = false;

            if (ImageListControl.StopSlideshowIfRunning())
            {
                refreshView = true;
            }

            if (!Config.SourceDirectories.Equals(sourceDirectories))
            {
                Config.SourceDirectories = sourceDirectories;
                _movedDirectoriesToProcess = movedDirectories;
                refreshIncludedFlag = true;
                refreshFiles = true;
            }

            if (refreshIncludedFlag)
            {
                Database.RefreshIncludedFlag(Connection, sourceDirectories);
            }

            if (refreshFiles)
            {
                RefreshFiles();
            }
            else if (refreshView)
            {
                MainWindow.UpdateInfoBar();
            }
        }

        public static void ReloadFiles()
        {
            RefreshFiles();
        }

        private static void RefreshFiles()
        {
            MainWindow.AllowInput = false;

            var fileList = GetAllImages(Config.SourceDirectories);
            var orphanedImages = new List<OrphanedData>();
            var getIncludedItems = new SQLiteCommand("SELECT id, name, lastWriteTime, hash, images.lastRefreshDate AS imagesLastRefresh, summaries.lastRefreshDate AS summariesLastRefresh, ineligible, tags, rating FROM images LEFT JOIN summaries ON images.id = summaries.imageId WHERE included ORDER BY lower(name) ASC", Connection);
            var reader = getIncludedItems.ExecuteReader();
            var newImageId = Database.GetLastInsertId(Connection) + 1;

            using (var transaction = Connection.BeginTransaction())
            {
                var updateWriteTimeTime = new SQLiteParameter();
                var updateWriteTimeId = new SQLiteParameter();

                var updateWriteTime = new SQLiteCommand(Connection)
                {
                    CommandText = "UPDATE images SET lastWriteTime = ? WHERE id = ?"
                };
                updateWriteTime.Parameters.Add(updateWriteTimeTime);
                updateWriteTime.Parameters.Add(updateWriteTimeId);

                var updateNameName = new SQLiteParameter();
                var updateNameId = new SQLiteParameter();

                var updateName = new SQLiteCommand(Connection) {CommandText = "UPDATE images SET name = ? WHERE id = ?"};
                updateName.Parameters.Add(updateNameName);
                updateName.Parameters.Add(updateNameId);

                var insertNewImageId = new SQLiteParameter();
                var insertNewImageName = new SQLiteParameter();
                var insertNewImageLastWriteTime = new SQLiteParameter();
                var insertNewImageIncluded = new SQLiteParameter();

                var insertNewImage = new SQLiteCommand(Connection)
                {
                    CommandText = "INSERT INTO images (id, name, lastWriteTime, included) VALUES (?, ?, ?, ?)"
                };
                insertNewImage.Parameters.Add(insertNewImageId);
                insertNewImage.Parameters.Add(insertNewImageName);
                insertNewImage.Parameters.Add(insertNewImageLastWriteTime);
                insertNewImage.Parameters.Add(insertNewImageIncluded);

                insertNewImageIncluded.Value = true;

                var deleteOldImageId = new SQLiteParameter();
                var deleteOldImage = new SQLiteCommand(Connection) {CommandText = "DELETE FROM images WHERE id = ?"};
                deleteOldImage.Parameters.Add(deleteOldImageId);

                var deleteOldImageSummaryId = new SQLiteParameter();
                var deleteOldImageSummary = new SQLiteCommand(Connection)
                {
                    CommandText = "DELETE FROM summaries WHERE imageId = ?"
                };
                deleteOldImageSummary.Parameters.Add(deleteOldImageSummaryId);

                var i = 0;
                bool rowRead;

                while ((rowRead = reader.Read()) || i < fileList.Count)
                {
                    while (i < fileList.Count && (!rowRead || string.CompareOrdinal(fileList[i].FullName.ToLowerInvariant(), reader.GetString("name").ToLowerInvariant()) < 0))
                    {
                        // Add new image to main table
                        insertNewImageId.Value = newImageId;
                        insertNewImageName.Value = fileList[i].FullName;
                        insertNewImageLastWriteTime.Value = fileList[i].LastWriteTime;
                        insertNewImage.ExecuteNonQuery();
                        newImageId++;
                        i++;
                    }

                    if (!rowRead)
                    {
                        continue;
                    }

                    var databaseName = reader.GetString("name");
                    var databaseId = reader.GetInt32("id");

                    if (i >= fileList.Count || fileList[i].FullName.ToLowerInvariant() != databaseName.ToLowerInvariant()) {
                        // This item is orphaned, add it to the list to process later
                        var orphanedData = new OrphanedData {Name = databaseName, ImageId = databaseId};

                        if (!reader.IsDbNull("hash"))
                        {
                            orphanedData.Hash = reader.GetString("hash");
                        }

                        if (!reader.IsDbNull("tags"))
                        {
                            orphanedData.Tags = reader.GetString("tags");
                        }

                        if (!reader.IsDbNull("rating"))
                        {
                            orphanedData.Rating = reader.GetInt32("rating");
                        }
                        orphanedImages.Add(orphanedData);

                        // Remove original database entry for item
                        deleteOldImageId.Value = databaseId;
                        deleteOldImage.ExecuteNonQuery();
                        deleteOldImageSummaryId.Value = databaseId;
                        deleteOldImageSummary.ExecuteNonQuery();
                    }
                    else
                    {
                        // Update LastWriteTime and ensure hash/image summary is up to date
                        if (reader.IsDbNull("lastWriteTime") ||
                            reader.GetDateTime("lastWriteTime") != fileList[i].LastWriteTime)
                        {
                            updateWriteTimeId.Value = databaseId;
                            updateWriteTimeTime.Value = fileList[i].LastWriteTime;
                            updateWriteTime.ExecuteNonQuery();
                        }

                        if (databaseName != fileList[i].FullName)
                        {
                            updateNameName.Value = fileList[i].FullName;
                            updateNameId.Value = databaseId;
                            updateName.ExecuteNonQuery();
                        }
                        i++;
                    }
                }
                reader.Dispose();
                transaction.Commit();
            }
            MainWindow.SetPicture(null);
            MainWindow.ProgressBarVisible = true;
            FinishFilesRefresh(orphanedImages);
            MainWindow.AllowInput = true;
        }

        private static void FinishFilesRefresh(List<OrphanedData> orphanedImages)
        {
            MainWindow.ProgressBarVisible = false;
            Database.ApplyOrphanedData(orphanedImages, Connection);

            if (_movedDirectoriesToProcess != null && _movedDirectoriesToProcess.Count > 0)
            {
                using (var transaction = Connection.BeginTransaction())
                {
                    var deleteOldImageId = new SQLiteParameter();
                    var deleteOldImage = new SQLiteCommand(Connection) {CommandText = "DELETE FROM images WHERE id = ?"};
                    deleteOldImage.Parameters.Add(deleteOldImageId);

                    var deleteOldImageSummaryId = new SQLiteParameter();
                    var deleteOldImageSummary = new SQLiteCommand(Connection)
                    {
                        CommandText = "DELETE FROM summaries WHERE imageId = ?"
                    };
                    deleteOldImageSummary.Parameters.Add(deleteOldImageSummaryId);

                    foreach (var movedDirectory in _movedDirectoriesToProcess)
                    {
                        var getMissingData = new SQLiteCommand("SELECT id, name, tags, rating, hash FROM images WHERE name LIKE \"" + movedDirectory + "%\"", Connection);
                        using (var reader = getMissingData.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tags = null;
                                if (!reader.IsDbNull("tags"))
                                {
                                    tags = reader.GetString("tags");
                                }
                                var rating = 0;
                                if (!reader.IsDbNull("rating"))
                                {
                                    rating = reader.GetInt32("rating");
                                }
                                string hash = null;
                                if (!reader.IsDbNull("hash"))
                                {
                                    hash = reader.GetString("hash");
                                }
                                var hasTags = tags != null && tags.Length > 2;
                                if (hasTags || rating > 0)
                                {
                                    var orphanedData = new OrphanedData { Name = reader.GetString("name") };

                                    if (hasTags)
                                    {
                                        orphanedData.Tags = tags;
                                    }

                                    if (rating > 0)
                                    {
                                        orphanedData.Rating = rating;
                                    }

                                    if (!string.IsNullOrEmpty(hash))
                                    {
                                        orphanedData.Hash = hash;
                                    }
                                }
                                var oldImageId = reader.GetInt32("id");
                                deleteOldImageId.Value = oldImageId;
                                deleteOldImage.ExecuteNonQuery();

                                deleteOldImageSummaryId.Value = oldImageId;
                                deleteOldImageSummary.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
            Database.RebuildTagsSummary();
            ImageListControl.RunImageQueryForced();
        }

        private static List<FileInfo> GetAllImages(DirectorySet directorySet)
        {
            var fileList = new List<FileInfo>();

            var directoriesChanged = false;

            foreach (var baseDirectory in directorySet.BaseDirectories)
            {
                if (baseDirectory.Missing != !Directory.Exists(baseDirectory.Path))
                {
                    directoriesChanged = true;

                    baseDirectory.Missing = !Directory.Exists(baseDirectory.Path);
                }
            }

            if (directoriesChanged)
            {
                Config.SourceDirectories = directorySet;
                Database.RefreshIncludedFlag(Connection, directorySet);
            }

            foreach (var baseDirectory in directorySet.BaseDirectories)
            {
                if (!baseDirectory.Missing)
                {
                    AddImages(baseDirectory, fileList, new DirectoryInfo(baseDirectory.Path));
                }
            }
            return fileList;
        }

        private static void AddImages(BaseDirectory baseDirectory, List<FileInfo> fileList, DirectoryInfo currentDir)
        {
            foreach (var extension in Extensions)
            {
                fileList.AddRange(currentDir.GetFiles("*." + extension));
            }

            try
            {
                var subDirectories = currentDir.GetDirectories();

                foreach (var subDirectory in subDirectories)
                {
                    if (!baseDirectory.Exclusions.Contains(subDirectory.FullName.ToLowerInvariant()))
                    {
                        AddImages(baseDirectory, fileList, subDirectory);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Do nothing. This will ignore reading images from inaccessible folders.
            }
        }
    }
}