using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using SmartPhotoOrganizer.DataStructures;
using SmartPhotoOrganizer.InputRelated;

namespace SmartPhotoOrganizer.DatabaseOp
{
    public static class Database
    {
        private const string GalleryDb = "SmartPhotoGallerySqlite.db";

        public static string DbFile => GalleryDb;
        public static string ConnectionString => "Data Source=" + DbFile;
        private static Dictionary<string, int> _tagsSummary;

        public static void CreateTables(SQLiteConnection connection)
        {
            // Table: Images
            ExecuteNonQuery("CREATE TABLE images (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, tags TEXT, rating INTEGER, lastWriteTime TEXT, included BOOL, hash TEXT, lastRefreshDate DATETIME)", connection);
            ExecuteNonQuery("CREATE UNIQUE INDEX idIndex ON images (id)", connection);
            ExecuteNonQuery("CREATE INDEX hashIndex ON images (hash)", connection);

            // Table: View Lists
            ExecuteNonQuery("CREATE TABLE viewLists (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, listIndex INTEGER, orderBy TEXT, ascending BOOL, rating INTEGER, searchString TEXT, searchTags BOOL, searchFileName BOOL, untagged BOOL, customClause TEXT)", connection);

            // Table: View Settings
            ExecuteNonQuery("CREATE TABLE settings (name TEXT, value TEXT)", connection);

            // Table: Base Directories
            ExecuteNonQuery("CREATE TABLE baseDirectories (id INTEGER PRIMARY KEY AUTOINCREMENT, path TEXT, missing BOOL)", connection);

            // Table: Excluded Directories
            ExecuteNonQuery("CREATE TABLE excludedDirectories (id INTEGER PRIMARY KEY AUTOINCREMENT, baseId INTEGER, path TEXT)", connection);

            // Table: Input Mapping
            ExecuteNonQuery("CREATE TABLE inputMapping (inputCode INTEGER, inputType INTEGER, actionCode INTEGER)", connection);

            // Table: Summaries
            ExecuteNonQuery("CREATE TABLE summaries (imageId INTEGER, ineligible BOOL, lastRefreshDate DATETIME, thumbnail BINARY, averageVariance DOUBLE, fractionDrasticPixelBorders DOUBLE, hashPrefix TEXT)", connection);
            ExecuteNonQuery("CREATE UNIQUE INDEX imageIdIndex ON summaries (imageId)", connection);
            ExecuteNonQuery("CREATE INDEX hashPrefixIndex ON summaries (hashPrefix, ineligible)", connection);
        }

        public static void PopulateDefaultConfig(SQLiteConnection connection, DirectorySet sourceDirectories)
        {
            using (var transaction = connection.BeginTransaction())
            {
                // Add default view list
                InsertDefaultViewList(connection);

                // Add source directory set
                DbConfig.UpdateSourceDirectories(sourceDirectories, connection);

                // Add default config settings
                AddSettingsList(connection, DbConfigAccess.ConfigDefaults);

                // Add default input mappers
                InputMapper.InsertDefaultsIntoMapping(connection);

                // Commit changes
                transaction.Commit();
            }
        }

        private static void InsertDefaultViewList(SQLiteConnection connection)
        {
            var insertDefaultList = new SQLiteCommand(connection)
            {
                CommandText = "INSERT INTO viewLists (id, name, listIndex, orderBy, ascending, rating, searchString, searchTags, searchFileName, untagged, customClause) VALUES (1, 'Default', 0, 'random', ?, 0, '', ?, ?, ?, '')"
            };

            var ascendingParam = new SQLiteParameter { Value = false };
            insertDefaultList.Parameters.Add(ascendingParam);

            var searchTagsParam = new SQLiteParameter { Value = false };
            insertDefaultList.Parameters.Add(searchTagsParam);

            var searchFileNameParam = new SQLiteParameter { Value = false };
            insertDefaultList.Parameters.Add(searchFileNameParam);

            var untaggedParam = new SQLiteParameter { Value = false };
            insertDefaultList.Parameters.Add(untaggedParam);

            insertDefaultList.ExecuteNonQuery();
        }

        public static void ApplyOrphanedData(List<OrphanedData> orphanedImages, SQLiteConnection connection)
        {
            // Find new location for orphaned data.
            if (orphanedImages.Count <= 0) return;
            var getImageFromHash = new SQLiteCommand("SELECT id FROM images WHERE hash = ? AND included", connection);
            var hashParam = new SQLiteParameter();
            getImageFromHash.Parameters.Add(hashParam);

            foreach (var orphanedData in orphanedImages)
            {
                if (!string.IsNullOrEmpty(orphanedData.Hash))
                {
                    hashParam.Value = orphanedData.Hash;
                    using (var reader = getImageFromHash.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            orphanedData.ImageId = reader.GetInt32("id");
                            orphanedData.FoundOwner = true;
                        }
                    }
                }
                else
                {
                    var getImageFromName = new SQLiteCommand("SELECT id FROM images WHERE name LIKE \"%\\" + Path.GetFileName(orphanedData.Name) + "\" AND included", connection);
                    using (var reader = getImageFromName.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            orphanedData.ImageId = reader.GetInt32("id");
                            orphanedData.FoundOwner = true;
                        }
                    }
                }
            }

            // Apply the orphaned data to the found matches
            using (var transaction = connection.BeginTransaction())
            {
                var updateImageTags = new SQLiteParameter();
                var updateImageRating = new SQLiteParameter();
                var updateImageId = new SQLiteParameter();

                var updateImage = new SQLiteCommand(connection)
                {
                    CommandText = "UPDATE images SET tags = ?, rating = ? WHERE id = ?"
                };

                updateImage.Parameters.Add(updateImageTags);
                updateImage.Parameters.Add(updateImageRating);
                updateImage.Parameters.Add(updateImageId);

                foreach (var orphanedData in orphanedImages)
                {
                    if (!orphanedData.FoundOwner) continue;
                    updateImageTags.Value = orphanedData.Tags;
                    updateImageRating.Value = orphanedData.Rating;
                    updateImageId.Value = orphanedData.ImageId;
                    updateImage.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public static string GetImageName(SQLiteConnection connection, int imageId)
        {
            var getCommand = new SQLiteCommand("SELECT name FROM images WHERE id = " + imageId, connection);

            using (var reader = getCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.GetString("name");
                }
            }
            return null;
        }

        public static void SetImageName(SQLiteConnection connection, int imageId, string newName)
        {
            var updateName = new SQLiteCommand("UPDATE images SET name = \"" + newName + "\" WHERE id = " + imageId, connection);
            updateName.ExecuteNonQuery();
        }

        public static ImageUserData GetImageUserData(SQLiteConnection connection, int imageId)
        {
            var getCommand = new SQLiteCommand("SELECT id, name, tags, rating FROM images WHERE id = " + imageId, connection);

            using (var reader = getCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    var result = new ImageUserData
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name")
                    };

                    if (!reader.IsDbNull("tags"))
                    {
                        result.Tags = Utilities.ListFromPipeList(reader.GetString("tags"));
                    }
                    if (!reader.IsDbNull("rating"))
                    {
                        result.Rating = reader.GetInt32("rating");
                    }
                    return result;
                }
            }
            return null;
        }

        public static void SetRating(SQLiteConnection connection, int rating, int imageId)
        {
            var updateRating = new SQLiteCommand("UPDATE images SET rating = " + rating + " WHERE id = " + imageId, connection);
            updateRating.ExecuteNonQuery();
        }

        public static void RebuildTagsSummary()
        {
            _tagsSummary = null;
        }

        public static void RemoveImageData(int imageToRemove, SQLiteConnection connection)
        {
            var imagesToRemove = new List<int> {imageToRemove};

            DecrementRemovedImages(imagesToRemove, connection);

            using (var transaction = connection.BeginTransaction())
            {
                var deleteImage = new SQLiteCommand("DELETE FROM images WHERE id = " + imageToRemove, connection);
                deleteImage.ExecuteNonQuery();

                var deleteImageSummary = new SQLiteCommand("DELETE FROM summaries WHERE imageId = " + imageToRemove, connection);
                deleteImageSummary.ExecuteNonQuery();

                transaction.Commit();
            }
        }

        public static void RemoveImageData(List<int> imagesToRemove, SQLiteConnection connection)
        {
            DecrementRemovedImages(imagesToRemove, connection);

            using (var transaction = connection.BeginTransaction())
            {
                var idParameter = new SQLiteParameter();
                var removeImage = new SQLiteCommand(connection) {CommandText = "DELETE FROM images WHERE id = ?"};
                removeImage.Parameters.Add(idParameter);

                var idSummaryParameter = new SQLiteParameter();
                var removeImageSummary = new SQLiteCommand(connection)
                {
                    CommandText = "DELETE FROM summaries WHERE imageId = ?"
                };
                removeImageSummary.Parameters.Add(idSummaryParameter);

                foreach (var imageId in imagesToRemove)
                {
                    idParameter.Value = imageId;
                    removeImage.ExecuteNonQuery();

                    idSummaryParameter.Value = imageId;
                    removeImageSummary.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        private static void DecrementRemovedImages(List<int> removedImages, SQLiteConnection connection)
        {
            if (_tagsSummary == null) return;
            var getTagsFromImages = new SQLiteCommand("SELECT tags, included FROM images WHERE id = ?", connection);
            var getTagsImageIdParam = getTagsFromImages.Parameters.Add("imageId", System.Data.DbType.Int32);

            foreach (var imageId in removedImages)
            {
                getTagsImageIdParam.Value = imageId;
                using (var reader = getTagsFromImages.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (!reader.IsDbNull("included") && reader.GetBoolean("included") && !reader.IsDbNull("tags"))
                        {
                            var tags = Utilities.ListFromPipeList(reader.GetString("tags"));

                            foreach (var tag in tags)
                            {
                                DecrementTagSummary(tag);
                            }
                        }
                    }
                }
            }
        }

        public static void IncrementTagSummary(string tag)
        {
            if (_tagsSummary == null)
            {
                return;
            }
            if (!_tagsSummary.ContainsKey(tag))
            {
                _tagsSummary.Add(tag, 0);
            }
            _tagsSummary[tag]++;
        }

        public static void DecrementTagSummary(string tag)
        {
            if (_tagsSummary == null)
            {
                return;
            }
            if (!_tagsSummary.ContainsKey(tag)) return;
            _tagsSummary[tag]--;

            if (_tagsSummary[tag] == 0)
            {
                _tagsSummary.Remove(tag);
            }
        }

        public static List<string> GetTags(SQLiteConnection connection, int imageId)
        {
            var getTags = new SQLiteCommand("SELECT tags FROM images WHERE id = " + imageId, connection);
            using (var reader = getTags.ExecuteReader())
            {
                if (reader.Read() && !reader.IsDbNull("tags"))
                {
                    return Utilities.ListFromPipeList(reader.GetString("tags"));
                }
                return new List<string>();
            }
        }

        public static Dictionary<string, int> GetTagsSummary(SQLiteConnection connection)
        {
            if (_tagsSummary != null) return _tagsSummary;
            _tagsSummary = new Dictionary<string, int>();

            var selectTags = new SQLiteCommand("SELECT tags FROM images WHERE included = 1", connection);
            using (var reader = selectTags.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.IsDbNull("tags")) continue;
                    var tags = Utilities.ListFromPipeList(reader.GetString("tags"));
                    foreach (var tag in tags)
                    {
                        IncrementTagSummary(tag);
                    }
                }
            }

            return _tagsSummary;
        }

        public static void SetTags(SQLiteConnection connection, List<string> tags, int imageId)
        {
            var updateTags = new SQLiteCommand("UPDATE images SET tags = '" + Utilities.PipeListFromList(tags) + "' WHERE id = " + imageId, connection);
            updateTags.ExecuteNonQuery();
        }

        public static void SetTags(SQLiteConnection connection, List<string> tags, int imageId, List<string> oldTags)
        {
            foreach (var tag in oldTags)
            {
                DecrementTagSummary(tag);
            }
            foreach (var tag in tags)
            {
                IncrementTagSummary(tag);
            }
            SetTags(connection, tags, imageId);
        }

        public static void AddTagToImages(SQLiteConnection connection, string tag, List<int> images)
        {
            // First get all the tags.
            var tagList = GetMultipleTags(connection, images);

            // Now, add the tag to all the tag collections
            foreach (var tags in tagList.Values)
            {
                if (tags.Contains(tag)) continue;
                IncrementTagSummary(tag);
                tags.Add(tag);
            }

            // Finally, write out the new batch of tags
            SetMultipleTags(connection, tagList);
        }

        public static void RemoveTagFromImages(SQLiteConnection connection, string tag, List<int> images)
        {
            // First get all the tags.
            var tagList = GetMultipleTags(connection, images);

            // Now, add the tag to all the tag collections
            foreach (var tags in tagList.Values)
            {
                if (!tags.Contains(tag)) continue;
                DecrementTagSummary(tag);
                tags.Remove(tag);
            }

            // Finally, write out the new batch of tags
            SetMultipleTags(connection, tagList);
        }

        public static Dictionary<int, List<string>> GetMultipleTags(SQLiteConnection connection, List<int> images)
        {
            var result = new Dictionary<int, List<string>>();
            var idParameter = new SQLiteParameter();
            var getTags = new SQLiteCommand(connection) {CommandText = "SELECT tags FROM images WHERE id = ?"};
            getTags.Parameters.Add(idParameter);

            foreach (var imageId in images)
            {
                idParameter.Value = imageId;
                using (var reader = getTags.ExecuteReader())
                {
                    if (reader.Read() && !reader.IsDbNull("tags"))
                    {
                        result.Add(imageId, Utilities.ListFromPipeList(reader.GetString("tags")));
                    }
                    else
                    {
                        result.Add(imageId, new List<string>());
                    }
                }
            }
            return result;
        }

        public static void SetMultipleTags(SQLiteConnection connection, Dictionary<int, List<string>> newTags)
        {
            using (var transaction = connection.BeginTransaction())
            {
                var tagParameter = new SQLiteParameter();
                var idParameter = new SQLiteParameter();

                var updateTags = new SQLiteCommand(connection)
                {
                    CommandText = "UPDATE images SET tags = ? WHERE id = ?"
                };
                updateTags.Parameters.Add(tagParameter);
                updateTags.Parameters.Add(idParameter);

                foreach (var pair in newTags)
                {
                    tagParameter.Value = Utilities.PipeListFromList(pair.Value);
                    idParameter.Value = pair.Key;
                    updateTags.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public static void RenameTag(string oldTag, string newTag, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                var updateTags = new SQLiteCommand("UPDATE images SET tags = ? WHERE id = ?", connection);
                var updateTagsTagsParam = updateTags.Parameters.Add("tags", System.Data.DbType.String);
                var updateTagsIdParam = updateTags.Parameters.Add("id", System.Data.DbType.Int32);

                var getTags = new SQLiteCommand("SELECT tags, id FROM images WHERE tags LIKE '%|" + oldTag + "|%' AND included = 1", connection);
                using (var reader = getTags.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDbNull("tags"))
                        {
                            var tags = Utilities.ListFromPipeList(reader.GetString("tags"));
                            var oldTagLocation = tags.IndexOf(oldTag);

                            tags.RemoveAt(oldTagLocation);
                            tags.Insert(oldTagLocation, newTag);
                            updateTagsTagsParam.Value = Utilities.PipeListFromList(tags);

                            updateTagsIdParam.Value = reader.GetInt32("id");
                            updateTags.ExecuteNonQuery();
                        }
                    }
                }
                transaction.Commit();
            }
        }

        public static void DeleteTag(string tagToDelete, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                var deleteTag = new SQLiteCommand("UPDATE images SET tags = ? WHERE id = ?", connection);
                var deleteTagTagsParam = deleteTag.Parameters.Add("tags", System.Data.DbType.String);
                var deleteTagIdParam = deleteTag.Parameters.Add("id", System.Data.DbType.Int32);

                var getTags = new SQLiteCommand("SELECT tags, id FROM images WHERE tags LIKE '%|" + tagToDelete + "|%' AND included = 1", connection);
                using (var reader = getTags.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDbNull("tags"))
                        {
                            var tags = Utilities.ListFromPipeList(reader.GetString("tags"));
                            tags.Remove(tagToDelete);
                            deleteTagTagsParam.Value = Utilities.PipeListFromList(tags);
                            deleteTagIdParam.Value = reader.GetInt32("id");
                            deleteTag.ExecuteNonQuery();
                        }
                    }
                }
                transaction.Commit();
            }
        }

        public static long GetLastInsertId(SQLiteConnection connection)
        {
            var getLastIdCommand = new SQLiteCommand("SELECT max(id) AS lastInsertId FROM images", connection);
            var lastIdObject = getLastIdCommand.ExecuteScalar();

            try
            {
                return (long) lastIdObject;
            }
            catch (InvalidCastException)
            {
                return -1L;
            }
        }

        private static void AddSettingsList(SQLiteConnection connection, Dictionary<string, string> settingsList)
        {
            using (var settingsInsert = new SQLiteCommand(connection))
            {
                // Add settings
                var settingsName = new SQLiteParameter();
                var settingsValue = new SQLiteParameter();

                settingsInsert.CommandText = "INSERT INTO settings (name, value) VALUES (?, ?)";
                settingsInsert.Parameters.Add(settingsName);
                settingsInsert.Parameters.Add(settingsValue);

                foreach (var pair in settingsList)
                {
                    settingsName.Value = pair.Key;
                    settingsValue.Value = pair.Value;
                    settingsInsert.ExecuteNonQuery();
                }
            }
        }

        public static void RefreshIncludedFlag(SQLiteConnection connection, DirectorySet sourceDirectories)
        {
            using (var transaction = connection.BeginTransaction())
            {
                var getState = new SQLiteCommand("SELECT id, name, included FROM images", connection);
                using (var reader = getState.ExecuteReader())
                {
                    var includedParam = new SQLiteParameter();
                    var idParam = new SQLiteParameter();
                    var updateIncludedFlag = new SQLiteCommand(connection)
                    {
                        CommandText = "UPDATE images SET included = ? WHERE id = ?"
                    };
                    updateIncludedFlag.Parameters.Add(includedParam);
                    updateIncludedFlag.Parameters.Add(idParam);

                    while (reader.Read())
                    {
                        var fileDirectory = Path.GetDirectoryName(reader.GetString("name"));

                        var oldIncluded = reader.GetBoolean("included");
                        var newIncluded = sourceDirectories.PathIsIncluded(fileDirectory);

                        if (newIncluded == oldIncluded) continue;
                        includedParam.Value = newIncluded;
                        idParam.Value = reader.GetInt32("id");
                        updateIncludedFlag.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
        }

        public static void ExecuteNonQuery(string query, SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = query;
                command.ExecuteNonQuery();
            }
        }
    }
}
