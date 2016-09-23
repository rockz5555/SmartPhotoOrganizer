using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for EditTagBox.xaml
    /// </summary>
    public partial class EditTagBox
    {
        private readonly List<string> _previousTags;
        private readonly TagAutoCompleteTextBox _tagBox;

        public EditTagBox(MainWindow window)
        {
            Owner = window;
            InitializeComponent();
            var tagsSummary = Database.GetTagsSummary(PhotoManager.Connection);
            _tagBox = new TagAutoCompleteTextBox {TagsSummary = tagsSummary, Margin = new Thickness(53, 10, 23, 0)};
            RootGrid.Children.Add(_tagBox);
            _previousTags = PhotoManager.CurrentTags;

            if (_previousTags.Count > 0)
            {
                var tagsBuilder = new StringBuilder();
                foreach (var tag in _previousTags)
                {
                    tagsBuilder.Append(tag);
                    tagsBuilder.Append(" ");
                }
                var tagsText = tagsBuilder.ToString();
                _tagBox.TextBox.Text = tagsText;
                _tagBox.TextBox.SelectionStart = tagsText.Length;
                _tagBox.TextBox.SelectionLength = 0;
            }
            _tagBox.TextBox.Focus();
        }

        private void tagEditOk_Click(object sender, RoutedEventArgs e)
        {
            var tagString = _tagBox.TextBox.Text;
            var tags = tagString.Split(' ');
            var tagsResult = (from tag in tags select tag.Replace("|", "") into cleanTag select cleanTag.Replace(",", "") into cleanTag where cleanTag != "" select cleanTag.ToLowerInvariant()).ToList();
            TagOperations.UpdateCurrentTags(_previousTags, tagsResult);
            DialogResult = true;
        }
    }
}
