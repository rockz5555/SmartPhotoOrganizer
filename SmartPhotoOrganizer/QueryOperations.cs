using System.Data.SQLite;
using SmartPhotoOrganizer.DataStructures;

namespace SmartPhotoOrganizer
{
    public static class QueryOperations
    {
        public static int CurrentListId { get; set; }

        public static void SetImageList(int listId)
        {
            var command = new SQLiteCommand("SELECT name, orderBy, ascending, rating, searchString, searchTags, searchFileName, untagged, customClause FROM viewLists WHERE id = " + listId, PhotoManager.Connection);
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                var orderBy = reader.GetString("orderBy");

                switch (orderBy)
                {
                    case "random":
                        ImageQuery.Sort = SortType.Random;
                        break;
                    case "name":
                        ImageQuery.Sort = SortType.Name;
                        break;
                    case "lastWriteTime":
                        ImageQuery.Sort = SortType.Modified;
                        break;
                }
                ImageQuery.Ascending = reader.GetBoolean("ascending");
                ImageQuery.MinRating = reader.GetInt32("rating");
                ImageQuery.Search = reader.GetString("searchString");
                ImageQuery.SearchTags = reader.GetBoolean("searchTags");
                ImageQuery.SearchFileName = reader.GetBoolean("searchFileName");
                ImageQuery.UntaggedOnly = reader.GetBoolean("untagged");
                ImageQuery.CustomClause = reader.GetString("customClause");
                ImageQuery.ListName = reader.GetString("name");
            }
        }

        public static void ShowImageList(int listId)
        {
            SetImageList(listId);
            ImageListControl.RunImageQuery(0);
        }

        public static void RunSearch(string searchTerm, bool searchFileName, bool searchTags)
        {
            PhotoManager.Config.SearchFileName = searchFileName;
            PhotoManager.Config.SearchTags = searchTags;
            ImageQuery.SearchFileName = searchFileName;
            ImageQuery.SearchTags = searchTags;
            ImageQuery.Search = searchTerm;

            ImageListControl.RunImageQuery(0);
        }

        public static void ChangeOrder()
        {
            switch (ImageQuery.Sort)
            {
                case SortType.Modified:
                    ImageQuery.Sort = SortType.Random;
                    break;
                case SortType.Random:
                    ImageQuery.Sort = SortType.Name;
                    ImageQuery.Ascending = true;
                    break;
                default:
                    ImageQuery.Sort = SortType.Modified;
                    ImageQuery.Ascending = false;
                    break;
            }
            ImageListControl.RunImageQuery(0);
        }

        public static void ClearSearch()
        {
            ImageQuery.Search = "";
            ImageQuery.CustomClause = "";
            ImageListControl.RunImageQuery(0);
        }

        public static void SetRatingView(int rating)
        {
            ImageQuery.MinRating = rating;
            ImageListControl.RunImageQuery(0);
        }

        public static void ToggleUntagged()
        {
            ImageQuery.UntaggedOnly = !ImageQuery.UntaggedOnly;
            ImageListControl.RunImageQuery(0);
        }
    }
}
