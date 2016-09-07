using System.Data.SQLite;

namespace SmartPhotoOrganizer
{
    public static class Database
    {
        private const string GalleryDb = "SmartPhotoGallerySqliteDb.db";

        public static string DbFile => GalleryDb;
        public static string ConnectionString => "Data Source=" + DbFile;

        public static void CreateTables(SQLiteConnection connection)
        {
            // Table 1
            ExecuteNonQuery("CREATE TABLE images (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, tags TEXT, rating INTEGER, lastWriteTime TEXT, hash TEXT)", connection);
            ExecuteNonQuery("CREATE UNIQUE INDEX idIndex ON images (id)", connection);
            ExecuteNonQuery("CREATE INDEX hashIndex ON images (hash)", connection);

            // Table 2
            ExecuteNonQuery("CREATE TABLE baseDirectories (id INTEGER PRIMARY KEY AUTOINCREMENT, path TEXT, missing BOOL)", connection);
            ExecuteNonQuery("CREATE TABLE excludedDirectories (id INTEGER PRIMARY KEY AUTOINCREMENT, baseId INTEGER, path TEXT)", connection);
        }

        public static void PopulateDefaultConfig(SQLiteConnection connection, DirectorySet sourceDirectories)
        {
            using (var transaction = connection.BeginTransaction())
            {
                // Add source directory set
                DbConfig.UpdateSourceDirectories(sourceDirectories, connection);
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
