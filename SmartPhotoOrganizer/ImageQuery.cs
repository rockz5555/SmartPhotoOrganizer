using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using SmartPhotoOrganizer.DataStructures;

namespace SmartPhotoOrganizer
{
    public static class ImageQuery
    {
        private static bool _filtersChanged = true;
        private static bool _untaggedOnly;
        private static string _search = "";
        private static bool _searchFileName;
        private static bool _searchTags;
        private static int _minRating;
        private static string _customClause = "";
        private static SortType _sort = SortType.Random;
        private static bool _ascending;
        private static string _listName = "";

        public static bool UntaggedOnly
        {
            get { return _untaggedOnly; }
            set
            {
                if (value != _untaggedOnly)
                {
                    _untaggedOnly = value;
                    MarkChanged();
                }
            }
        }

        public static string Search
        {
            get { return _search; }
            set
            {
                if (value.ToLowerInvariant() != _search)
                {
                    _search = value.ToLowerInvariant();
                    MarkChanged();
                }
            }
        }

        public static bool SearchFileName
        {
            get { return _searchFileName; }
            set
            {
                if (value != _searchFileName)
                {
                    _searchFileName = value;
                    MarkChanged();
                }
            }
        }

        public static bool SearchTags
        {
            get { return _searchTags; }
            set
            {
                if (value != _searchTags)
                {
                    _searchTags = value;
                    MarkChanged();
                }
            }
        }

        public static int MinRating
        {
            get { return _minRating; }
            set
            {
                if (value != _minRating)
                {
                    _minRating = value;
                    MarkChanged();
                }
            }
        }

        public static string CustomClause
        {
            get { return _customClause; }
            set
            {
                if (value != _customClause)
                {
                    _customClause = value;
                    MarkChanged();
                }
            }
        }

        public static SortType Sort
        {
            get { return _sort; }
            set
            {
                if (value != _sort)
                {
                    _sort = value;
                    MarkChanged();
                }
            }
        }

        public static bool Ascending
        {
            get { return _ascending; }
            set
            {
                if (value != _ascending)
                {
                    _ascending = value;
                    MarkChanged();
                }
            }
        }

        public static string ListName
        {
            get { return _listName; }
            set { _listName = value; }
        }

        public static bool HasFilter
        {
            get
            {
                return _search != "" || _untaggedOnly || _minRating != 0 || _customClause != "";
            }
        }

        public static List<int> GetImageList(SQLiteConnection connection, bool forceRefresh)
        {
            if (forceRefresh)
            {
                _filtersChanged = true;
            }

            return GetImageList(connection);
        }

        public static List<int> GetImageList(SQLiteConnection connection)
        {
            if (!_filtersChanged)
            {
                return null;
            }

            var imageList = new List<int>();

            var selectCommand = new SQLiteCommand(CreateSelectStatement(), connection);
            using (var reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    imageList.Add(reader.GetInt32("id"));
                }
            }

            return imageList;
        }

        private static string CreateSelectStatement()
        {
            var builder = new StringBuilder();
            builder.Append("SELECT id FROM images WHERE included");

            var clauses = new List<string>();

            if (_search != "" && (_searchFileName || _searchTags))
            {
                var searchTerms = _search.Split(new char[] { ' ' });

                foreach (var rawSearchTerm in searchTerms)
                {
                    var exclude = false;

                    var searchTerm = rawSearchTerm.Trim();
                    searchTerm = searchTerm.Replace("'", "");
                    searchTerm = searchTerm.Replace("|", "");

                    if (searchTerm != "" && searchTerm[0] == '-')
                    {
                        exclude = true;
                        searchTerm = searchTerm.Substring(1, searchTerm.Length - 1);
                    }

                    if (searchTerm == "")
                    {
                        continue;
                    }

                    if (!exclude)
                    {
                        if (_searchFileName && _searchTags)
                        {
                            clauses.Add("(tags LIKE '%|" + searchTerm + "|%' OR name LIKE '%" + searchTerm + "%')");
                        }
                        else if (_searchFileName)
                        {
                            clauses.Add("name LIKE '%" + searchTerm + "%'");
                        }
                        else if (_searchTags)
                        {
                            clauses.Add("(tags LIKE '%|" + searchTerm + "|%')");
                        }
                    }
                    else
                    {
                        if (_searchFileName && _searchTags)
                        {
                            clauses.Add("tags NOT LIKE '%|" + searchTerm + "|%' AND name NOT LIKE '%" + searchTerm + "%'");
                        }
                        else if (_searchFileName)
                        {
                            clauses.Add("name NOT LIKE '%" + searchTerm + "%'");
                        }
                        else if (_searchTags)
                        {
                            clauses.Add("tags NOT LIKE '%|" + searchTerm + "|%'");
                        }
                    }
                }
            }

            if (_untaggedOnly)
            {
                clauses.Add("(tags ISNULL OR tags = '')");
            }

            if (_minRating != 0)
            {
                if (_minRating == -1)
                {
                    clauses.Add("(rating ISNULL OR rating = 0)");
                }
                else
                {
                    clauses.Add("rating >= " + _minRating);
                }
            }

            if (_customClause != "")
            {
                clauses.Add(_customClause);
            }

            if (clauses.Count > 0)
            {
                foreach (var clause in clauses)
                {
                    builder.Append(" AND ");
                    builder.Append(clause);
                }
            }

            string orderClause;
            var ascDscString = _ascending ? "ASC" : "DESC";

            switch (_sort)
            {
                case SortType.Random:
                    orderClause = " ORDER BY RANDOM()";
                    break;
                case SortType.Modified:
                    orderClause = " ORDER BY lastWriteTime " + ascDscString;
                    break;
                case SortType.Name:
                    orderClause = " ORDER BY name " + ascDscString;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            builder.Append(orderClause);

            return builder.ToString();
        }

        private static void MarkChanged()
        {
            _filtersChanged = true;
            _listName = "";
        }
    }
}