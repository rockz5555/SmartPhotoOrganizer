using System;
using System.Data.SQLite;
using System.IO;
using SmartPhotoOrganizer.UIAspects;

namespace SmartPhotoOrganizer
{
    public static class PhotoManager
    {
        public static MainWindow MainInterface;
        private static SQLiteConnection _sqLiteConnection;

        public static void Initiate(MainWindow mainWindow)
        {
            MainInterface = mainWindow;
            var newDb = !File.Exists(Database.DbFile);

            if (!newDb) return;

            DirectorySet sourceDirectories = null;
            var helloWindow = new Hello();
            var showDialog = helloWindow.ShowDialog();

            if (showDialog != null && (bool)showDialog)
            {
                sourceDirectories = helloWindow.SourceDirectories;
            }
            else
            {
                Environment.Exit(0);
            }

            _sqLiteConnection = new SQLiteConnection(Database.ConnectionString);
            _sqLiteConnection.Open();

            Database.CreateTables(_sqLiteConnection);
            Database.PopulateDefaultConfig(_sqLiteConnection, sourceDirectories);
        }
    }
}
