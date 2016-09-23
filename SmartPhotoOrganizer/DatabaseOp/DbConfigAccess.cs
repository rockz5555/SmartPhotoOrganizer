using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;

namespace SmartPhotoOrganizer.DatabaseOp
{
    public static class DbConfigAccess
    {
        public static Dictionary<string, string> ConfigDefaults { get; } = new Dictionary<string, string>();

        static DbConfigAccess()
        {
            ConfigDefaults.Add("LastSettingsTab", "1");
            ConfigDefaults.Add("SearchFileName", "True");
            ConfigDefaults.Add("SearchTags", "True");
            ConfigDefaults.Add("StartingListId", "1");
            AddWindowSizeDefaults(ConfigDefaults,
                "Main",
                "Duplicate",
                "ViewLists",
                "Settings",
                "EditKeys",
                "AllDuplicates",
                "ImageDetail",
                "TagRenameOrDelete");
            ConfigDefaults.Add("ExpandedDirectories", string.Empty);
            ConfigDefaults.Add("FullscreenStartSetting", "0");
            ConfigDefaults.Add("FullscreenStart", "True");
            ConfigDefaults.Add("OverlayInfobar", "True");
            ConfigDefaults.Add("SlideshowDelaySeconds", "5");
        }

        private static void AddWindowSizeDefaults(Dictionary<string, string> configDefaults, params string[] windowNames)
        {
            foreach (var windowName in windowNames)
            {
                configDefaults.Add(windowName + "Width", "0");
                configDefaults.Add(windowName + "Height", "0");
            }
        }

        public static bool GetConfigBool(string configName, SQLiteConnection connection)
        {
            return bool.Parse(GetConfigString(configName, connection));
        }

        public static double GetConfigDouble(string configName, SQLiteConnection connection)
        {
            return double.Parse(GetConfigString(configName, connection));
        }

        public static int GetConfigInt(string configName, SQLiteConnection connection)
        {
            return int.Parse(GetConfigString(configName, connection));
        }

        public static string GetConfigString(string configName, SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand("SELECT value FROM settings WHERE name = '" + configName + "'", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0);
                    }
                }
            }

            // The setting does not exist in the DB, we need to insert the default value.
            if (!ConfigDefaults.ContainsKey(configName))
                throw new ArgumentException(@"Config value not found: " + configName, nameof(configName));
            AddConfigValue(configName, ConfigDefaults[configName], connection);

            return ConfigDefaults[configName];
        }

        private static void AddConfigValue(string configName, string configValue, SQLiteConnection connection)
        {
            using (var settingsInsert = new SQLiteCommand(connection))
            {
                var settingsName = new SQLiteParameter();
                var settingsValue = new SQLiteParameter();

                settingsInsert.CommandText = "INSERT INTO settings (name, value) VALUES (?, ?)";
                settingsInsert.Parameters.Add(settingsName);
                settingsInsert.Parameters.Add(settingsValue);
                settingsName.Value = configName;
                settingsValue.Value = configValue;
                settingsInsert.ExecuteNonQuery();
            }
        }

        public static void SetConfigValue(string configName, bool value, SQLiteConnection connection)
        {
            SetConfigValue(configName, value.ToString(), connection);
        }

        public static void SetConfigValue(string configName, double value, SQLiteConnection connection)
        {
            SetConfigValue(configName, value.ToString(CultureInfo.InvariantCulture), connection);
        }

        public static void SetConfigValue(string configName, int value, SQLiteConnection connection)
        {
            SetConfigValue(configName, value.ToString(), connection);
        }

        public static void SetConfigValue(string configName, string configValue, SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand("UPDATE settings SET value = \"" + configValue + "\" WHERE name = '" + configName + "'", connection))
            {
                if (command.ExecuteNonQuery() == 0)
                {
                    // If the setting did not exist, add it
                    AddConfigValue(configName, configValue, connection);
                }
            }
        }
    }
}
