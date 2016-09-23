using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Windows;
using SmartPhotoOrganizer.DataStructures;

namespace SmartPhotoOrganizer.DatabaseOp
{
    public class DbConfig
    {
        private readonly SQLiteConnection _connection;
        private readonly Dictionary<string, WindowSize> _cachedWindowSizes;
        private DirectorySet _sourceDirectories;
        private bool _searchFileName;
        private bool _searchTags;
        private int _startingListId;
        private string _expandedDirectories;
        private FullscreenStart _fullscreenStart;
        private bool _overlayInfobar;
        private int _slideshowDelaySeconds;

        public DbConfig(SQLiteConnection connection)
        {
            _connection = connection;
            _cachedWindowSizes = new Dictionary<string, WindowSize>();
            _searchFileName = GetConfigBool("SearchFileName");
            _searchTags = GetConfigBool("SearchTags");
            _startingListId = GetConfigInt("StartingListId");
            _expandedDirectories = GetConfigString("ExpandedDirectories");
            _fullscreenStart = (FullscreenStart) GetConfigInt("FullscreenStartSetting");
            _overlayInfobar = GetConfigBool("OverlayInfobar");
            _slideshowDelaySeconds = GetConfigInt("SlideshowDelaySeconds");
        }

        public DirectorySet SourceDirectories
        {
            get
            {
                if (_sourceDirectories != null) return _sourceDirectories;
                _sourceDirectories = new DirectorySet();

                var getBaseDirectories = new SQLiteCommand("SELECT id, path, missing FROM baseDirectories", _connection);
                using (var reader = getBaseDirectories.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var baseDirectory = new BaseDirectory
                        {
                            Path = reader.GetString("path"),
                            Missing = reader.GetBoolean("missing")
                        };


                        var getExcludedDirectories = new SQLiteCommand("SELECT path FROM excludedDirectories WHERE baseId = " + reader.GetInt32("id"), _connection);
                        using (var excludedReader = getExcludedDirectories.ExecuteReader())
                        {
                            while (excludedReader.Read())
                            {
                                baseDirectory.Exclusions.Add(excludedReader.GetString("path"));
                            }
                        }

                        _sourceDirectories.BaseDirectories.Add(baseDirectory);
                    }
                }

                return _sourceDirectories;
            }
            set
            {
                UpdateSourceDirectories(value, _connection);
                _sourceDirectories = value;
            }
        }

        public static void UpdateSourceDirectories(DirectorySet sourceDirectories, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                Database.ExecuteNonQuery("DELETE FROM baseDirectories", connection);
                Database.ExecuteNonQuery("DELETE FROM excludedDirectories", connection);

                var insertBaseDirectory = new SQLiteCommand("INSERT INTO baseDirectories (id, path, missing) VALUES (?, ?, ?)", connection);
                var baseIdParam = new SQLiteParameter();
                var basePathParam = new SQLiteParameter();
                var baseMissingParam = new SQLiteParameter();

                insertBaseDirectory.Parameters.Add(baseIdParam);
                insertBaseDirectory.Parameters.Add(basePathParam);
                insertBaseDirectory.Parameters.Add(baseMissingParam);

                var insertExcludedDirectory = new SQLiteCommand("INSERT INTO excludedDirectories (baseId, path) VALUES (?, ?)", connection);
                var excludedIdParam = new SQLiteParameter();
                var excludedPathParam = new SQLiteParameter();

                insertExcludedDirectory.Parameters.Add(excludedIdParam);
                insertExcludedDirectory.Parameters.Add(excludedPathParam);

                var baseDirId = 1;

                foreach (var baseDirectory in sourceDirectories.BaseDirectories)
                {
                    baseIdParam.Value = baseDirId;
                    basePathParam.Value = baseDirectory.Path;
                    baseMissingParam.Value = baseDirectory.Missing;

                    insertBaseDirectory.ExecuteNonQuery();

                    foreach (var excludedDirectory in baseDirectory.Exclusions)
                    {
                        excludedIdParam.Value = baseDirId;
                        excludedPathParam.Value = excludedDirectory;

                        insertExcludedDirectory.ExecuteNonQuery();
                    }
                    baseDirId++;
                }
                transaction.Commit();
            }
        }

        public bool SearchFileName
        {
            get { return _searchFileName; }

            set
            {
                if (value != _searchFileName)
                {
                    _searchFileName = value;
                    SetConfigValue("SearchFileName", value);
                }
            }
        }

        public bool SearchTags
        {
            get { return _searchTags; }

            set
            {
                if (value != _searchTags)
                {
                    _searchTags = value;
                    SetConfigValue("SearchTags", value);
                }
            }
        }

        public int StartingListId
        {
            get { return _startingListId; }

            set
            {
                if (value != _startingListId)
                {
                    _startingListId = value;
                    SetConfigValue("StartingListId", value);
                }
            }
        }

        public List<string> ExpandedDirectories
        {
            get
            {
                var parts = _expandedDirectories.Split('|');
                return new List<string>(parts);
            }

            set
            {
                var result = new StringBuilder();

                for (var i = 0; i < value.Count; i++)
                {
                    result.Append(value[i]);

                    if (i != value.Count - 1)
                    {
                        result.Append("|");
                    }
                }

                _expandedDirectories = result.ToString();
                SetConfigValue("ExpandedDirectories", _expandedDirectories);
            }
        }

        public FullscreenStart FullscreenStartSetting
        {
            get { return _fullscreenStart; }

            set
            {
                if (value == _fullscreenStart) return;
                _fullscreenStart = value;
                SetConfigValue("FullscreenStartSetting", (int) value);
            }
        }

        public bool OverlayInfobar
        {
            get { return _overlayInfobar; }

            set
            {
                if (value == _overlayInfobar) return;
                _overlayInfobar = value;
                SetConfigValue("OverlayInfobar", value);
            }
        }

        public int SlideshowDelaySeconds
        {
            get { return _slideshowDelaySeconds; }

            set
            {
                if (value == _slideshowDelaySeconds) return;
                _slideshowDelaySeconds = value;
                SetConfigValue("SlideshowDelaySeconds", value);
            }
        }

        public bool FullscreenStart
        {
            get { return GetConfigBool("FullscreenStart"); }

            set { SetConfigValue("FullscreenStart", value); }
        }

        public void ApplyWindowSize(string windowName, Window window)
        {
            if (!_cachedWindowSizes.ContainsKey(windowName))
            {
                var newWindowSize = new WindowSize
                {
                    Width = GetConfigDouble(windowName + "Width"),
                    Height = GetConfigDouble(windowName + "Height")
                };
                _cachedWindowSizes.Add(windowName, newWindowSize);
            }

            if (_cachedWindowSizes[windowName].Width > 0)
            {
                window.Width = _cachedWindowSizes[windowName].Width;
            }

            if (_cachedWindowSizes[windowName].Height > 0)
            {
                window.Height = _cachedWindowSizes[windowName].Height;
            }
        }

        public void SaveWindowSize(string windowName, Window window)
        {
            var width = window.ActualWidth;
            var height = window.ActualHeight;

            if (_cachedWindowSizes.ContainsKey(windowName))
            {
                if (_cachedWindowSizes[windowName].Width == width && _cachedWindowSizes[windowName].Height == height)
                {
                    return;
                }
                _cachedWindowSizes.Remove(windowName);
            }

            SetConfigValue(windowName + "Width", width);
            SetConfigValue(windowName + "Height", height);

            _cachedWindowSizes.Add(windowName, new WindowSize {Width = width, Height = height});
        }

        public bool GetConfigBool(string configName)
        {
            return DbConfigAccess.GetConfigBool(configName, _connection);
        }

        public double GetConfigDouble(string configName)
        {
            return DbConfigAccess.GetConfigDouble(configName, _connection);
        }

        public int GetConfigInt(string configName)
        {
            return DbConfigAccess.GetConfigInt(configName, _connection);
        }

        public string GetConfigString(string configName)
        {
            return DbConfigAccess.GetConfigString(configName, _connection);
        }

        public void SetConfigValue(string configName, bool value)
        {
            DbConfigAccess.SetConfigValue(configName, value, _connection);
        }

        public void SetConfigValue(string configName, double value)
        {
            DbConfigAccess.SetConfigValue(configName, value, _connection);
        }

        public void SetConfigValue(string configName, int value)
        {
            DbConfigAccess.SetConfigValue(configName, value, _connection);
        }

        public void SetConfigValue(string configName, string configValue)
        {
            DbConfigAccess.SetConfigValue(configName, configValue, _connection);
        }
    }
}
