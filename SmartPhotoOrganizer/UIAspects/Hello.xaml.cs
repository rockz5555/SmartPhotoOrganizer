using System.Windows;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for Hello.xaml
    /// </summary>
    public partial class Hello
    {
        public Hello()
        {
            InitializeComponent();
        }

        public DirectorySet SourceDirectories => MultiFolderPicker.DirectorySet;

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (MultiFolderPicker.HasDirectories)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select at least a single image folder", "Forgot to add a folder?", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
