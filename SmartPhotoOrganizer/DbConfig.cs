using System.Data.SQLite;

namespace SmartPhotoOrganizer
{
    public class DbConfig
    {
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
    }
}
