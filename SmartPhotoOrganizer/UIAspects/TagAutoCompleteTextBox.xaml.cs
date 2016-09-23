using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for TagAutoCompleteTextBox.xaml
    /// </summary>
    public partial class TagAutoCompleteTextBox
    {
        private const int MaxSuggestedTags = 20;
        private bool _autoCompleteEnabled = true;

        public TagAutoCompleteTextBox()
        {
            InitializeComponent();
        }

        public Dictionary<string, int> TagsSummary { get; set; }

        public TextBox TextBox => Tags;

        public string Text
        {
            get
            {
                return Tags.Text;
            }
            set
            {
                Tags.Text = value;
            }
        }

        public bool AutoCompleteEnabled
        {
            get
            {
                return _autoCompleteEnabled;
            }
            set
            {
                _autoCompleteEnabled = value;
                if (!value)
                {
                    TagPopup.IsOpen = false;
                }
            }
        }

        public bool LimitToOneTag { get; set; }

        public bool IgnoreDashPrefix { get; set; }

        private void tags_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_autoCompleteEnabled)
            {
                return;
            }

            var caretIndex = Tags.CaretIndex;

            if (caretIndex == 0 || Tags.Text[caretIndex - 1] == ' ')
            {
                TagPopup.IsOpen = false;
                return;
            }

            var tagsCurrentlyInBox = new List<string>();

            var beforeCaretText = Tags.Text.Substring(0, caretIndex);
            var beforeCaretWords = beforeCaretText.Split(' ');

            if (LimitToOneTag && beforeCaretWords.Length > 1)
            {
                TagPopup.IsOpen = false;
                return;
            }

            for (var i = 0; i < beforeCaretWords.Length - 1; i++)
            {
                if (beforeCaretWords[i] != string.Empty)
                {
                    tagsCurrentlyInBox.Add(beforeCaretWords[i]);
                }
            }

            var lastWordBeforeCaret = beforeCaretWords[beforeCaretWords.Length - 1].ToLowerInvariant();

            if (lastWordBeforeCaret == string.Empty)
            {
                return;
            }

            if (IgnoreDashPrefix)
            {
                if (lastWordBeforeCaret[0] == '-')
                {
                    lastWordBeforeCaret = lastWordBeforeCaret.Substring(1);
                }

                if (lastWordBeforeCaret == string.Empty)
                {
                    return;
                }
            }

            var afterCaretText = Tags.Text.Substring(caretIndex);
            var afterCaretWords = afterCaretText.Split(' ');

            for (var i = 1; i < afterCaretWords.Length; i++)
            {
                if (afterCaretWords[i] != string.Empty)
                {
                    tagsCurrentlyInBox.Add(afterCaretWords[i]);
                }
            }

            var autoCompleteTags = new List<TagWithFrequency>();

            var sortedTags = from p in TagsSummary where p.Key.StartsWith(lastWordBeforeCaret) orderby p.Value descending select p;

            foreach (var pair in sortedTags)
            {
                if (!tagsCurrentlyInBox.Contains(pair.Key))
                {
                    autoCompleteTags.Add(new TagWithFrequency { Tag = pair.Key, Frequency = pair.Value });
                }

                if (autoCompleteTags.Count == MaxSuggestedTags)
                {
                    break;
                }
            }

            if (autoCompleteTags.Count == 0)
            {
                TagPopup.IsOpen = false;
                return;
            }

            TagPopup.IsOpen = true;
            TagListBox.ItemsSource = autoCompleteTags;
            TagListBox.SelectedIndex = 0;

            var textSize = Tags.GetRectFromCharacterIndex(Tags.CaretIndex);

            TagPopup.PlacementTarget = Tags;
            TagPopup.PlacementRectangle = textSize;
        }

        private void ListItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var sendingItem = sender as ListBoxItem;
            var dataContext = sendingItem.DataContext as TagWithFrequency;
            CompleteCurrentTag(dataContext.Tag);
        }

        private void tags_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (TagPopup.IsOpen && TagListBox.SelectedIndex > 0)
                    {
                        TagListBox.SelectedIndex--;
                    }
                    break;
                case Key.Down:
                    if (TagPopup.IsOpen && TagListBox.SelectedIndex < TagListBox.Items.Count - 1)
                    {
                        TagListBox.SelectedIndex++;
                    }
                    break;
                case Key.Tab:
                    if (TagPopup.IsOpen)
                    {
                        CompleteCurrentTag(((TagWithFrequency) TagListBox.SelectedItem).Tag);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void CompleteCurrentTag(string fullTag)
        {
            var caretIndex = Tags.CaretIndex;
            var beforeCaretText = Tags.Text.Substring(0, caretIndex);
            var beforeCaretWords = beforeCaretText.Split(' ');
            var lastWordBeforeCaret = beforeCaretWords[beforeCaretWords.Length - 1];

            if (IgnoreDashPrefix && lastWordBeforeCaret[0] == '-')
            {
                lastWordBeforeCaret = lastWordBeforeCaret.Substring(1);
            }

            var postSpaceText = Tags.Text.Substring(caretIndex, Tags.Text.Length - caretIndex);
            var newTag = new StringBuilder();

            newTag.Append(Tags.Text.Substring(0, caretIndex - lastWordBeforeCaret.Length));
            newTag.Append(fullTag);

            var addSpace = postSpaceText.Length > 0 || !LimitToOneTag;

            if (addSpace)
            {
                newTag.Append(" ");
            }

            newTag.Append(postSpaceText);
            Tags.Text = newTag.ToString();

            var newCaretIndex = caretIndex - lastWordBeforeCaret.Length + fullTag.Length;

            if (addSpace)
            {
                newCaretIndex++;
            }
            Tags.CaretIndex = newCaretIndex;
            TagPopup.IsOpen = false;
        }

        private void tagPopup_Closed(object sender, EventArgs e)
        {
            Tags.Focus();
        }
    }
}
