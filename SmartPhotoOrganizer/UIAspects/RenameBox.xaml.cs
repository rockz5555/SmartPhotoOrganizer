using System;
using System.Text;
using System.Windows;
using System.IO;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for RenameBox.xaml
    /// </summary>
    public partial class RenameBox
    {
        private readonly string _initialPath;
        private readonly ImageUserData _userData;

        public RenameBox(MainWindow mainWindow)
        {
            Owner = mainWindow;
            _userData = ImageListControl.CurrentImageData;
            InitializeComponent();
            _initialPath = PhotoManager.CurrentImageName;
            RenameText.Text = Path.GetFileName(_initialPath);

            RenameText.SelectionStart = 0;
            RenameText.SelectionLength = 0;

            RenameText.Focus();

            if (_userData.Tags == null || _userData.Tags.Count == 0)
            {
                AutoName.IsEnabled = false;
            }
        }

        private void RenameClick(object sender, RoutedEventArgs e)
        {
            EditOperations.RenameCurrentImage(RenameText.Text);
            DialogResult = true;
        }

        private void AutoNameClick(object sender, RoutedEventArgs e)
        {
            var extension = Path.GetExtension(_initialPath);
            var initialFileName = Path.GetFileName(_initialPath);
            var folder = _initialPath.Substring(0, _initialPath.Length - initialFileName.Length);

            var newNameBase = string.Join("-", _userData.Tags.ToArray());

            var newNameCandidate = newNameBase + extension;

            if (!File.Exists(Path.Combine(folder, newNameCandidate)))
            {
                RenameText.Text = newNameCandidate;
                return;
            }

            for (var i = 1; i < 50; i++)
            {
                newNameCandidate = newNameBase + "-" + i + extension;
                if (!File.Exists(Path.Combine(folder, newNameCandidate)))
                {
                    RenameText.Text = newNameCandidate;
                    return;
                }
            }

            RenameText.Text = newNameBase + "-" + GetRandomString(8) + extension;
        }

        private static string GetRandomString(int length)
        {
            const string randomCharacters = "0123456789abcdefghijklmnopqrstuvwxyz";
            var builder = new StringBuilder();
            var rand = new Random();

            for (var i = 0; i < length; i++)
            {
                builder.Append(randomCharacters[rand.Next(randomCharacters.Length)]);
            }
            return builder.ToString();
        }
    }
}