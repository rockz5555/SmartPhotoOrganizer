using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer
{
    public static class EditOperations
    {
        public static void RenameCurrentImage(string newName)
        {
            var oldName = PhotoManager.CurrentImageName;
            var newNameWithDir = Path.Combine(Path.GetDirectoryName(oldName), newName);

            try
            {
                File.Move(oldName, newNameWithDir);
                Database.SetImageName(PhotoManager.Connection, ImageListControl.CurrentImageId, newNameWithDir);
                PhotoManager.MainWindow.UpdateInfoBar();
            }
            catch (IOException)
            {
                HandleMoveException();
            }
            catch (UnauthorizedAccessException)
            {
                HandleMoveException();
            }
        }

        public static void RateImage(int rating)
        {
            if (ImageCache.Current.Image == null) return;
            Database.SetRating(PhotoManager.Connection, rating, ImageListControl.CurrentImageId);
            PhotoManager.MainWindow.UpdateInfoBar();
        }

        public static void MoveVisibleFiles()
        {
            var config = PhotoManager.Config;
            var connection = PhotoManager.Connection;

            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = @"Choose where to move the images."
            };
            if (config.SourceDirectories.BaseDirectories.Count == 1)
            {
                folderDialog.SelectedPath = config.SourceDirectories.BaseDirectories[0].Path;
            }
            if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            var targetFolder = folderDialog.SelectedPath;
            var targetFolderIncluded = config.SourceDirectories.PathIsIncluded(targetFolder);

            if (!targetFolderIncluded && MessageBox.Show("The directory you specified is not included in your image collection. Do you still want to move the files there?", "Confirm Move", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            var numFilesMoved = 0;
            var filesFailed = new List<string>();

            using (var transaction = connection.BeginTransaction())
            {
                var updateImageName = new SQLiteCommand("UPDATE images SET name = ? WHERE id = ?", connection);
                var updateNameParam = updateImageName.Parameters.Add("name", System.Data.DbType.String);
                var updateIdParam = updateImageName.Parameters.Add("id", System.Data.DbType.Int32);

                var disincludeImage = new SQLiteCommand("UPDATE images SET included = ? WHERE id = ?", connection);
                var disincludeParam = disincludeImage.Parameters.Add("included", System.Data.DbType.Boolean);
                var disincludeIdParam = disincludeImage.Parameters.Add("id", System.Data.DbType.Int32);
                disincludeParam.Value = false;

                var getImageName = new SQLiteCommand("SELECT name FROM images WHERE id = ?", connection);
                var getIdParam = getImageName.Parameters.Add("id", System.Data.DbType.Int32);

                foreach (var imageId in ImageListControl.ImageList)
                {
                    getIdParam.Value = imageId;
                    string oldPath;

                    using (var reader = getImageName.ExecuteReader())
                    {
                        reader.Read();
                        oldPath = reader.GetString("name");
                    }

                    var directoryName = Path.GetDirectoryName(oldPath);
                    if (directoryName != null && directoryName.ToLowerInvariant() != targetFolder.ToLowerInvariant())
                    {
                        var targetFile = Path.Combine(targetFolder, Path.GetFileName(oldPath));
                        try
                        {
                            if (!File.Exists(targetFile))
                            {
                                File.Move(oldPath, targetFile);
                            }
                            else
                            {
                                targetFile = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(oldPath) + "_" + Utilities.GetRandomString(5) + Path.GetExtension(oldPath));
                                File.Move(oldPath, targetFile);
                            }
                            updateNameParam.Value = targetFile;
                            updateIdParam.Value = imageId;
                            updateImageName.ExecuteNonQuery();
                            if (!targetFolderIncluded)
                            {
                                disincludeIdParam.Value = imageId;
                                disincludeImage.ExecuteNonQuery();
                            }
                            numFilesMoved++;
                        }
                        catch (FileNotFoundException)
                        {
                            // Ignore, if the file is not exist
                        }
                        catch (IOException)
                        {
                            filesFailed.Add(oldPath);
                        }
                    }
                }
                transaction.Commit();
            }

            var messageBuilder = new StringBuilder();

            if (numFilesMoved > 0)
            {
                messageBuilder.Append(numFilesMoved + " file(s) moved to " + targetFolder + " successfully.");
            }
            else
            {
                messageBuilder.Append("No files moved.");
            }

            if (filesFailed.Count > 0)
            {
                messageBuilder.Append(Environment.NewLine);
                messageBuilder.Append(Environment.NewLine);
                messageBuilder.Append("The following files were locked or in use and could not be moved:");

                foreach (var failedFile in filesFailed)
                {
                    messageBuilder.Append(Environment.NewLine);
                    messageBuilder.Append(failedFile);
                }
            }

            MessageBox.Show(messageBuilder.ToString());

            if (targetFolderIncluded)
            {
                PhotoManager.MainWindow.UpdateInfoBar();
            }
            else
            {
                ImageListControl.RunImageQuery(0);
            }
        }

        public static void CopyVisibleFiles()
        {
            var fileDropList = new StringCollection();

            foreach (var imageId in ImageListControl.ImageList)
            {
                var imagePath = Database.GetImageName(PhotoManager.Connection, imageId);

                if (File.Exists(imagePath))
                {
                    fileDropList.Add(imagePath);
                }
            }

            Clipboard.SetFileDropList(fileDropList);

            MessageBox.Show(fileDropList.Count + " images copied to clipboard. Use Paste in Windows Exporer to execute the copy.");
        }

        private static void HandleMoveException()
        {
            MessageBox.Show("Could not move file. Destination file already exists, or access is denied.", "Error with file move");
        }
    }
}
