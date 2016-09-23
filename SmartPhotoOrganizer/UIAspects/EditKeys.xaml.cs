using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Data.SQLite;
using SmartPhotoOrganizer.DatabaseOp;
using SmartPhotoOrganizer.InputRelated;
using InputType = SmartPhotoOrganizer.InputRelated.InputType;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for EditKeys.xaml
    /// </summary>
    public partial class EditKeys
    {
        public List<EditKeyRow> KeyRows { get; }
        private int _currentRow;
        private int _currentActionRow;
        private TextBlock _selectedBlock;
        public bool InCaptureMode { get; private set; }
        private Rectangle _frostRectangle;
        private readonly MouseButtonEventHandler _mouseButtonHandler;
        private readonly MouseWheelEventHandler _mouseWheelHandler;
        private TextBlock _captureHelpText;

        public EditKeys()
        {
            _mouseButtonHandler = HandleMouseClick;
            _mouseWheelHandler = HandleMouseWheel;
            InitializeComponent();
            PhotoManager.Config.ApplyWindowSize("EditKeys", this);
            CommandManager.AddCanExecuteHandler(this, CanExecuteHandler);
            KeyRows = new List<EditKeyRow>();
            AddKeysToGrid();
        }

        private void AddKeysToGrid()
        {
            _selectedBlock = null;

            ActionGrid.Children.Clear();
            ActionGrid.RowDefinitions.Clear();

            ActionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var actionHeader = new TextBlock
            {
                Text = "Action",
                Margin = new Thickness(1),
                Padding = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue
            };
            Grid.SetRow(actionHeader, 0);
            Grid.SetColumn(actionHeader, 0);
            ActionGrid.Children.Add(actionHeader);

            var keyHeader = new TextBlock
            {
                Text = "Key/Button",
                Margin = new Thickness(1),
                Padding = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue
            };
            Grid.SetRow(keyHeader, 0);
            Grid.SetColumn(keyHeader, 1);
            ActionGrid.Children.Add(keyHeader);

            _currentActionRow = 0;
            _currentRow = 1;
            AddUneditableAction("Show This Screen", "F1");
            AddUserAction(UserAction.ShowOptions);
            AddUserAction(UserAction.ReloadFilesFromDisk);
            AddUserAction(UserAction.ToggleFullscreen);
            AddUserAction(UserAction.Minimize);
            AddUserAction(UserAction.Quit);

            AddSpacer();

            AddUserAction(UserAction.RateAs1);
            AddUserAction(UserAction.RateAs2);
            AddUserAction(UserAction.RateAs3);
            AddUserAction(UserAction.RateAs4);
            AddUserAction(UserAction.RateAs5);
            AddUserAction(UserAction.ClearRating);
            AddUserAction(UserAction.Tag);
            AddUserAction(UserAction.TagEditMultiple);
            AddUserAction(UserAction.TagRenameOrDelete);
            AddUserAction(UserAction.Rename);
            AddUserAction(UserAction.Move);
            AddUserAction(UserAction.CopyFiles);
            AddUserAction(UserAction.DeleteCurrentFile);

            AddSpacer();

            AddUserAction(UserAction.ShowPreviousImage);
            AddUserAction(UserAction.ShowNextImage);
            AddUserAction(UserAction.MoveToFirstImage);
            AddUserAction(UserAction.MoveToLastImage);
            AddUserAction(UserAction.PlayStopSlideshow);
            AddUserAction(UserAction.ShowLists);
            AddUserAction(UserAction.ChangeOrder);
            AddUserAction(UserAction.Search);
            AddUserAction(UserAction.ClearSearch);
            AddUserAction(UserAction.ShowRating1OrGreater);
            AddUserAction(UserAction.ShowRating2OrGreater);
            AddUserAction(UserAction.ShowRating3OrGreater);
            AddUserAction(UserAction.ShowRating4OrGreater);
            AddUserAction(UserAction.ShowRating5OrGreater);
            AddUserAction(UserAction.ClearRatingFilter);
            AddUserAction(UserAction.ShowOnlyUnrated);
            AddUserAction(UserAction.ShowOnlyUntagged);

            ActionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        private void AddUserAction(UserAction action)
        {
            ActionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var actionBlock = new TextBlock
            {
                Text = InputMapper.GetFriendlyName(action) + ":",
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(2),
                Margin = new Thickness(1)
            };
            Grid.SetRow(actionBlock, _currentRow);
            Grid.SetColumn(actionBlock, 0);
            ActionGrid.Children.Add(actionBlock);

            var currentColumn = 1;
            var editKeyRow = new EditKeyRow { Action = action };

            var selectAction = new SQLiteCommand("SELECT * FROM inputMapping WHERE actionCode = " + (int)action, PhotoManager.Connection);

            using (var reader = selectAction.ExecuteReader())
            {
                while (reader.Read())
                {
                    var inputCode = reader.GetInt32("inputCode");
                    var inputType = (InputType) reader.GetInt32("inputType");
                    var inputCommand = new InputCommand { InputCode = inputCode, InputType = inputType };

                    if (currentColumn < 4)
                    {
                        var keyBlock = new TextBlock
                        {
                            Text = InputMapper.GetFriendlyName(inputCode, inputType),
                            Padding = new Thickness(2),
                            Margin = new Thickness(1)
                        };
                        Grid.SetRow(keyBlock, _currentRow);
                        Grid.SetColumn(keyBlock, currentColumn);
                        keyBlock.Tag = inputCommand;
                        ActionGrid.Children.Add(keyBlock);
                        keyBlock.MouseLeftButtonDown += TextBlockMouseDown;
                        keyBlock.MouseEnter += TextBlockMouseEnter;
                        keyBlock.MouseLeave += TextBlockMouseLeave;
                        editKeyRow.KeyBlocks.Add(keyBlock);
                    }

                    currentColumn++;
                }
            }

            // Fill in unused blocks with blank TextBlocks
            while (currentColumn < 4)
            {
                var blankBlock = new TextBlock { Text = "", Padding = new Thickness(2), Margin = new Thickness(1) };
                Grid.SetRow(blankBlock, _currentRow);
                Grid.SetColumn(blankBlock, currentColumn);
                ActionGrid.Children.Add(blankBlock);
                blankBlock.MouseLeftButtonDown += TextBlockMouseDown;
                blankBlock.MouseEnter += TextBlockMouseEnter;
                blankBlock.MouseLeave += TextBlockMouseLeave;
                editKeyRow.KeyBlocks.Add(blankBlock);

                currentColumn++;
            }

            // On alternate rows, add a light gray background
            if (_currentActionRow % 2 == 0)
            {
                var backgroundRectangle = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(250, 250, 250)) };
                Grid.SetRow(backgroundRectangle, _currentRow);
                Grid.SetColumn(backgroundRectangle, 0);
                Grid.SetColumnSpan(backgroundRectangle, 4);
                Panel.SetZIndex(backgroundRectangle, -1);
                ActionGrid.Children.Add(backgroundRectangle);
            }
            _currentRow++;
            _currentActionRow++;
            KeyRows.Add(editKeyRow);
        }

        private void AddUneditableAction(string actionName, params string[] keys)
        {
            ActionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var actionBlock = new TextBlock { Text = actionName + ":", FontWeight = FontWeights.Bold, Padding = new Thickness(2), Margin = new Thickness(1) };
            Grid.SetRow(actionBlock, _currentRow);
            Grid.SetColumn(actionBlock, 0);
            ActionGrid.Children.Add(actionBlock);

            for (var i = 0; i < keys.Length && i < 3; i++)
            {
                var keyBlock = new TextBlock { Text = keys[i], Padding = new Thickness(2), Margin = new Thickness(1), Foreground = Brushes.DarkGray };
                Grid.SetRow(keyBlock, _currentRow);
                Grid.SetColumn(keyBlock, i + 1);
                ActionGrid.Children.Add(keyBlock);
            }

            // On alternate rows, add a light gray background
            if (_currentActionRow % 2 == 0)
            {
                var backgroundRectangle = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(250, 250, 250)) };
                Grid.SetRow(backgroundRectangle, _currentRow);
                Grid.SetColumn(backgroundRectangle, 0);
                Grid.SetColumnSpan(backgroundRectangle, 4);
                Panel.SetZIndex(backgroundRectangle, -1);
                ActionGrid.Children.Add(backgroundRectangle);
            }
            _currentRow++;
            _currentActionRow++;
        }

        private void AddSpacer()
        {
            var spacerRowDef = new RowDefinition {Height = new GridLength(25)};
            ActionGrid.RowDefinitions.Add(spacerRowDef);
            _currentRow++;
        }

        private void TextBlockMouseDown(object sender, MouseButtonEventArgs e)
        {
            var block = (TextBlock)sender;
            if (e.ClickCount == 1)
            {
                if (_selectedBlock != null)
                {
                    _selectedBlock.Background = null;
                }

                block.Background = SharedUi.SelectColor;
                _selectedBlock = block;
            }
            if (e.ClickCount == 2)
            {
                PromptForNewKey();
            }
        }

        private void PromptForNewKey()
        {
            InCaptureMode = true;
            _frostRectangle = new Rectangle { Opacity = .80, Fill = Brushes.White };
            MainGrid.Children.Add(_frostRectangle);

            _captureHelpText = new TextBlock
            {
                Text = "Press a key or mouse button\n(Shift + Esc to cancel)",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                TextAlignment = TextAlignment.Center
            };
            MainGrid.Children.Add(_captureHelpText);
            MouseDown += _mouseButtonHandler;
            MouseWheel += _mouseWheelHandler;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            if (!InCaptureMode) return;
            if (e.Key == Key.Escape && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                EndKeyCaptureMode();
                e.Handled = true;
                return;
            }
            switch (e.Key)
            {
                case Key.System:
                    if (e.SystemKey != Key.LeftAlt && e.SystemKey != Key.RightAlt && e.SystemKey != Key.Tab && e.SystemKey != Key.Space)
                    {
                        SetNewKey((int) e.SystemKey, InputType.KeyboardAlt);
                        EndKeyCaptureMode();
                    }
                    e.Handled = true;
                    return;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.F1:
                    e.Handled = true;
                    return;
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                SetNewKey((int) e.Key, InputType.KeyboardCtrl);
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                SetNewKey((int) e.Key, InputType.KeyboardShift);
            }
            else
            {
                SetNewKey((int) e.Key, InputType.Keyboard);
            }
            EndKeyCaptureMode();
            e.Handled = true;
        }

        private void HandleMouseClick(object sender, MouseButtonEventArgs e)
        {
            SetNewKey((int) e.ChangedButton, InputType.Mouse);
            EndKeyCaptureMode();
            e.Handled = true;
        }

        private void HandleMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var clicks = e.Delta/-120;

            if (clicks > 0)
            {
                SetNewKey((int) MouseWheelAction.Down, InputType.MouseWheel);
            }
            else if (clicks < 0)
            {
                SetNewKey((int) MouseWheelAction.Up, InputType.MouseWheel);
            }
            EndKeyCaptureMode();
            e.Handled = true;
        }

        private void CanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!InCaptureMode) return;
            e.Handled = true;
            e.CanExecute = false;
        }

        private void SetNewKey(int inputCode, InputType inputType)
        {
            TextBlock targetBlock = null;
            var inputCommand = new InputCommand { InputCode = inputCode, InputType = inputType };
            var ourRow = false;
            var oldAction = UserAction.None;

            // See if this key is already being used.
            foreach (var keyRow in KeyRows)
            {
                ourRow = false;

                foreach (var textBlock in keyRow.KeyBlocks)
                {
                    if (Equals(textBlock, _selectedBlock))
                    {
                        ourRow = true;
                    }
                    if (textBlock.Tag != null && inputCommand.Equals((InputCommand) textBlock.Tag))
                    {
                        targetBlock = textBlock;
                        oldAction = keyRow.Action;
                    }
                }
                if (targetBlock != null)
                {
                    break;
                }
            }
            if (targetBlock != null)
            {
                if (ourRow)
                {
                    // If the old row is our row, just delete the old key.
                    targetBlock.Text = "";
                    targetBlock.Tag = null;
                }
                else
                {
                    if (MessageBox.Show(InputMapper.GetFriendlyName(inputCode, inputType) + " is currently assigned to " + InputMapper.GetFriendlyName(oldAction) + ". Do you want to reassign the key?", "Confirm Key Reassignment", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                    targetBlock.Text = "";
                    targetBlock.Tag = null;
                }
            }
            _selectedBlock.Text = InputMapper.GetFriendlyName(inputCode, inputType);
            _selectedBlock.Tag = inputCommand;
        }

        private void EndKeyCaptureMode()
        {
            InCaptureMode = false;
            MainGrid.Children.Remove(_frostRectangle);
            MainGrid.Children.Remove(_captureHelpText);
            MouseDown -= _mouseButtonHandler;
            MouseWheel -= _mouseWheelHandler;
        }

        private void TextBlockMouseEnter(object sender, MouseEventArgs e)
        {
            var block = (TextBlock) sender;

            if (!Equals(block, _selectedBlock))
            {
                block.Background = SharedUi.HoverColor;
            }
        }

        private void TextBlockMouseLeave(object sender, MouseEventArgs e)
        {
            var block = (TextBlock)sender;

            if (!Equals(block, _selectedBlock))
            {
                block.Background = null;
            }
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you really want to set all controls back to defaults?", "Confirm Reset", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                InputMapper.ResetToDefaults(PhotoManager.Connection);
                AddKeysToGrid();
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            using (var transaction = PhotoManager.Connection.BeginTransaction())
            {
                foreach (var keyRow in KeyRows)
                {
                    Database.ExecuteNonQuery("DELETE FROM inputMapping WHERE actionCode = " + (int) keyRow.Action, PhotoManager.Connection);
                    foreach (var textBlock in keyRow.KeyBlocks)
                    {
                        if (textBlock.Tag != null)
                        {
                            var inputCommand = (InputCommand) textBlock.Tag;

                            Database.ExecuteNonQuery("INSERT INTO inputMapping (inputCode, inputType, actionCode) VALUES (" + inputCommand.InputCode + ", " + (int) inputCommand.InputType + ", " + (int) keyRow.Action + ")", PhotoManager.Connection);
                        }
                    }
                }
                transaction.Commit();
            }
            DialogResult = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (InCaptureMode) return;
            if (e.Key == Key.Delete && _selectedBlock != null && _selectedBlock.Tag != null)
            {
                if (MessageBox.Show("Are you sure you want to unbind this key?", "Confirm Key Unbind", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _selectedBlock.Text = "";
                    _selectedBlock.Tag = null;
                }
            }
            else if (e.Key == Key.Enter && _selectedBlock != null)
            {
                PromptForNewKey();
                e.Handled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PhotoManager.Config.SaveWindowSize("EditKeys", this);
        }
    }
}