using System.Collections.Generic;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer
{
    public static class TagOperations
    {
        public static void UpdateCurrentTags(List<string> oldTags, List<string> newTags)
        {
            Database.SetTags(PhotoManager.Connection, newTags, ImageListControl.CurrentImageId, oldTags);
            PhotoManager.MainWindow.UpdateInfoBar();
        }

        public static void AddTagToVisible(string tags)
        {
            var firstTag = GetFirstTag(tags);
            if (firstTag.Contains("|") || firstTag == "") return;
            Database.AddTagToImages(PhotoManager.Connection, firstTag, ImageListControl.ImageList);
            PhotoManager.MainWindow.UpdateInfoBar();
        }

        public static void RemoveTagFromVisible(string tags)
        {
            var firstTag = GetFirstTag(tags);
            if (firstTag == null) return;
            Database.RemoveTagFromImages(PhotoManager.Connection, firstTag, ImageListControl.ImageList);
            PhotoManager.MainWindow.UpdateInfoBar();
        }

        public static void RenameTag(string oldTag, string newTag)
        {
            Database.RenameTag(oldTag, newTag, PhotoManager.Connection);
            Database.RebuildTagsSummary();
            PhotoManager.MainWindow.UpdateInfoBar();
        }

        public static void DeleteTag(string tagToDelete)
        {
            Database.DeleteTag(tagToDelete, PhotoManager.Connection);
            Database.RebuildTagsSummary();
            PhotoManager.MainWindow.UpdateInfoBar();
        }

        public static string GetFirstTag(string tags)
        {
            var firstTag = tags;
            var spaceIndex = tags.IndexOf(' ');

            if (spaceIndex >= 0)
            {
                firstTag = tags.Substring(0, spaceIndex);
            }
            return firstTag.Contains("|") || firstTag.Length == 0 ? null : firstTag.ToLowerInvariant();
        }
    }
}
