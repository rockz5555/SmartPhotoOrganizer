using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Input;
using SmartPhotoOrganizer.DatabaseOp;

namespace SmartPhotoOrganizer.InputRelated
{
    public class InputMapper
    {
        private static Dictionary<UserAction, string> _actionFriendlyNames;
        private static Dictionary<Key, string> _keyFriendlyNames;
        private static Dictionary<MouseButton, string> _mouseButtonFriendlyNames;

        private readonly Dictionary<Key, UserAction> _keyMapping;
        private readonly Dictionary<Key, UserAction> _keyMappingCtrl;
        private readonly Dictionary<Key, UserAction> _keyMappingShift;
        private readonly Dictionary<Key, UserAction> _keyMappingAlt;
        private readonly Dictionary<MouseButton, UserAction> _mouseMapping;
        private readonly Dictionary<MouseWheelAction, UserAction> _mouseWheelMapping;

        public InputMapper(SQLiteConnection connection)
        {
            _keyMapping = new Dictionary<Key, UserAction>();
            _keyMappingCtrl = new Dictionary<Key, UserAction>();
            _keyMappingShift = new Dictionary<Key, UserAction>();
            _keyMappingAlt = new Dictionary<Key, UserAction>();
            _mouseMapping = new Dictionary<MouseButton, UserAction>();
            _mouseWheelMapping = new Dictionary<MouseWheelAction, UserAction>();

            var selectInputMapping = new SQLiteCommand("SELECT * FROM inputMapping", connection);
            using (var reader = selectInputMapping.ExecuteReader())
            {
                while (reader.Read())
                {
                    var inputType = (InputType) reader.GetInt32("inputType");
                    var action = (UserAction) reader.GetInt32("actionCode");
                    var inputCode = reader.GetInt32("inputCode");

                    switch (inputType)
                    {
                        case InputType.Mouse:
                            _mouseMapping.Add((MouseButton) inputCode, action);
                            break;
                        case InputType.MouseWheel:
                            _mouseWheelMapping.Add((MouseWheelAction) inputCode, action);
                            break;
                        case InputType.Keyboard:
                            _keyMapping.Add((Key) inputCode, action);
                            break;
                        case InputType.KeyboardCtrl:
                            _keyMappingCtrl.Add((Key) inputCode, action);
                            break;
                        case InputType.KeyboardShift:
                            _keyMappingShift.Add((Key) inputCode, action);
                            break;
                        case InputType.KeyboardAlt:
                            _keyMappingAlt.Add((Key) inputCode, action);
                            break;
                    }
                }
            }
        }

        private static Dictionary<UserAction, string> ActionFriendlyNames
        {
            get
            {
                if (_actionFriendlyNames != null) return _actionFriendlyNames;
                _actionFriendlyNames = new Dictionary<UserAction, string>
                {
                    {UserAction.ShowHelp, "Show Help"},
                    {UserAction.ShowOptions, "Show Settings"},
                    {UserAction.ReloadFilesFromDisk, "Reload Files From Disk"},
                    {UserAction.ToggleFullscreen, "Toggle Fullscreen Mode"},
                    {UserAction.RateAs1, "Rate Image as a 1"},
                    {UserAction.RateAs2, "Rate Image as a 2"},
                    {UserAction.RateAs3, "Rate Image as a 3"},
                    {UserAction.RateAs4, "Rate Image as a 4"},
                    {UserAction.RateAs5, "Rate Image as a 5"},
                    {UserAction.ClearRating, "Clear Image Rating"},
                    {UserAction.Tag, "Edit Tags on Current Image"},
                    {UserAction.TagEditMultiple, "Edit Tags on All Visible Images"},
                    {UserAction.TagRenameOrDelete, "Rename or Delete Tags"},
                    {UserAction.Rename, "Rename File"},
                    {UserAction.Move, "Move All Visible Images"},
                    {UserAction.CopyFiles, "Copy Visible Images to Clipboard"},
                    {UserAction.DeleteCurrentFile, "Delete Current Image"},
                    {UserAction.ShowPreviousImage, "Show Previous Image"},
                    {UserAction.ShowNextImage, "Show Next Image"},
                    {UserAction.MoveToFirstImage, "Move To First Image"},
                    {UserAction.MoveToLastImage, "Move To Last Image"},
                    {UserAction.PlayStopSlideshow, "Play/Stop Slideshow"},
                    {UserAction.ShowLists, "Show Image Lists"},
                    {UserAction.ChangeOrder, "Change Sort Order"},
                    {UserAction.ClearSearch, "Clear Current Search"},
                    {UserAction.ShowRating1OrGreater, "Show Only Rating 1+"},
                    {UserAction.ShowRating2OrGreater, "Show Only Rating 2+"},
                    {UserAction.ShowRating3OrGreater, "Show Only Rating 3+"},
                    {UserAction.ShowRating4OrGreater, "Show Only Rating 4+"},
                    {UserAction.ShowRating5OrGreater, "Show Only Rating 5"},
                    {UserAction.ClearRatingFilter, "Show All Ratings"},
                    {UserAction.ShowOnlyUnrated, "Show Only Unrated Images"},
                    {UserAction.ShowOnlyUntagged, "Show Only Untagged Images"},
                };
                return _actionFriendlyNames;
            }
        }

        private static Dictionary<Key, string> KeyFriendlyNames
        {
            get
            {
                if (_keyFriendlyNames != null) return _keyFriendlyNames;
                _keyFriendlyNames = new Dictionary<Key, string>
                {
                    {Key.Left, "←"},
                    {Key.Right, "→"},
                    {Key.Up, "↑"},
                    {Key.Down, "↓"},
                    {Key.OemTilde, "~"},
                    {Key.Back, "Backspace"},
                    {Key.OemMinus, "-"},
                    {Key.OemPlus, "+"},
                    {Key.Return, "Enter"},
                    {Key.OemPipe, @"\"},
                    {Key.Escape, "Esc"},
                    {Key.PageUp, "Page Up"},
                    {Key.Next, "Page Down"},
                    {Key.Oem4, "["},
                    {Key.Oem6, "]"},
                    {Key.OemSemicolon, ";"},
                    {Key.Oem7, "'"},
                    {Key.OemComma, "<"},
                    {Key.OemPeriod, ">"},
                    {Key.Oem2, "/"},
                    {Key.D0, "0"},
                    {Key.D1, "1"},
                    {Key.D2, "2"},
                    {Key.D3, "3"},
                    {Key.D4, "4"},
                    {Key.D5, "5"},
                    {Key.D6, "6"},
                    {Key.D7, "7"},
                    {Key.D8, "8"},
                    {Key.D9, "9"},
                    {Key.NumPad0, "Numpad 0"},
                    {Key.NumPad1, "Numpad 1"},
                    {Key.NumPad2, "Numpad 2"},
                    {Key.NumPad3, "Numpad 3"},
                    {Key.NumPad4, "Numpad 4"},
                    {Key.NumPad5, "Numpad 5"},
                    {Key.NumPad6, "Numpad 6"},
                    {Key.NumPad7, "Numpad 7"},
                    {Key.NumPad8, "Numpad 8"},
                    {Key.NumPad9, "Numpad 9"},
                    {Key.Divide, "Numpad /"},
                    {Key.Multiply, "Numpad *"},
                    {Key.Subtract, "Numpad -"},
                    {Key.Add, "Numpad +"},
                    {Key.Decimal, "NumPad ."}
                };
                return _keyFriendlyNames;
            }
        }

        private static Dictionary<MouseButton, string> MouseButtonFriendlyNames
        {
            get
            {
                if (_mouseButtonFriendlyNames != null) return _mouseButtonFriendlyNames;
                _mouseButtonFriendlyNames = new Dictionary<MouseButton, string>
                {
                    {MouseButton.Left, "Mouse 1"},
                    {MouseButton.Right, "Mouse 2"},
                    {MouseButton.Middle, "Mouse 3"},
                    {MouseButton.XButton1, "Mouse 4"},
                    {MouseButton.XButton2, "Mouse 5"}
                };
                return _mouseButtonFriendlyNames;
            }
        }

        public static bool IsBound(int inputCode, InputType inputType, SQLiteConnection connection)
        {
            var inputTypeInt = (int) inputType;
            var isBoundCommand = new SQLiteCommand("SELECT COUNT(*) FROM inputMapping WHERE inputType = " + inputTypeInt + " AND inputCode = " + inputCode, connection);
            var numKeys = Convert.ToInt32(isBoundCommand.ExecuteScalar());
            return numKeys > 0;
        }

        public UserAction GetActionFromKey(Key key)
        {
            return !_keyMapping.ContainsKey(key) ? UserAction.None : _keyMapping[key];
        }

        public UserAction GetActionFromKeyCtrl(Key key)
        {
            return !_keyMappingCtrl.ContainsKey(key) ? UserAction.None : _keyMappingCtrl[key];
        }

        public UserAction GetActionFromKeyShift(Key key)
        {
            return !_keyMappingShift.ContainsKey(key) ? UserAction.None : _keyMappingShift[key];
        }

        public UserAction GetActionFromKeyAlt(Key key)
        {
            return !_keyMappingAlt.ContainsKey(key) ? UserAction.None : _keyMappingAlt[key];
        }

        public UserAction GetActionFromMouseButton(MouseButton mouseButton)
        {
            return !_mouseMapping.ContainsKey(mouseButton) ? UserAction.None : _mouseMapping[mouseButton];
        }

        public UserAction GetActionFromMouseWheel(MouseWheelAction mouseWheelAction)
        {
            return !_mouseWheelMapping.ContainsKey(mouseWheelAction) ? UserAction.None : _mouseWheelMapping[mouseWheelAction];
        }

        public static string GetFriendlyName(int inputCode, InputType inputType)
        {
            if (inputType == InputType.Mouse)
            {
                var mouseButton = ((MouseButton) inputCode);
                return MouseButtonFriendlyNames.ContainsKey(mouseButton) ? MouseButtonFriendlyNames[mouseButton] : mouseButton.ToString();
            }
            if (inputType == InputType.MouseWheel)
            {
                var wheelAction = (MouseWheelAction) inputCode;
                if (wheelAction == MouseWheelAction.Up)
                {
                    return "MWheel Up";
                }

                if (wheelAction == MouseWheelAction.Down)
                {
                    return "MWheel Down";
                }
            }
            if (inputType == InputType.Keyboard || inputType == InputType.KeyboardCtrl || inputType == InputType.KeyboardShift || inputType == InputType.KeyboardAlt)
            {
                var prefix = "";
                if (inputType == InputType.KeyboardCtrl)
                {
                    prefix = "Ctrl + ";
                }
                else if (inputType == InputType.KeyboardShift)
                {
                    prefix = "Shift + ";
                }
                else if (inputType == InputType.KeyboardAlt)
                {
                    prefix = "Alt + ";
                }

                var inputKey = (Key) inputCode;

                if (KeyFriendlyNames.ContainsKey(inputKey))
                {
                    return prefix + KeyFriendlyNames[inputKey];
                }
                return prefix + inputKey;
            }
            return null;
        }

        public static string GetFriendlyName(UserAction action)
        {
            return ActionFriendlyNames.ContainsKey(action) ? ActionFriendlyNames[action] : action.ToString();
        }

        public static void ResetToDefaults(SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                Database.ExecuteNonQuery("DELETE FROM inputMapping", connection);
                InsertDefaultsIntoMapping(connection);
                transaction.Commit();
            }
        }

        public static void InsertDefaultsIntoMapping(SQLiteConnection connection)
        {
            var adder = new MappingInsert(connection);

            adder.AddMapping(Key.F1, UserAction.ShowHelp);
            adder.AddMapping(Key.F4, UserAction.ShowOptions);
            adder.AddMapping(Key.F5, UserAction.ReloadFilesFromDisk);
            adder.AddMapping(MouseButton.Middle, UserAction.ToggleFullscreen);
            adder.AddMapping(Key.OemPipe, UserAction.Minimize);
            adder.AddMapping(Key.Escape, UserAction.Quit);
            adder.AddMapping(Key.D1, UserAction.RateAs1);
            adder.AddMapping(Key.D2, UserAction.RateAs2);
            adder.AddMapping(Key.D3, UserAction.RateAs3);
            adder.AddMapping(Key.D4, UserAction.RateAs4);
            adder.AddMapping(Key.D5, UserAction.RateAs5);
            adder.AddMapping(Key.OemTilde, UserAction.ClearRating);
            adder.AddMapping(Key.T, UserAction.Tag);
            adder.AddMapping((int) Key.T, InputType.KeyboardCtrl, UserAction.TagEditMultiple);
            adder.AddMapping(Key.R, UserAction.Rename);
            adder.AddMapping(Key.F2, UserAction.Rename);
            adder.AddMapping(Key.M, UserAction.Move);
            adder.AddMapping(Key.Delete, UserAction.DeleteCurrentFile);
            adder.AddMapping(Key.Left, UserAction.ShowPreviousImage);
            adder.AddMapping(MouseWheelAction.Up, UserAction.ShowPreviousImage);
            adder.AddMapping(Key.Right, UserAction.ShowNextImage);
            adder.AddMapping(MouseWheelAction.Down, UserAction.ShowNextImage);
            adder.AddMapping(Key.Home, UserAction.MoveToFirstImage);
            adder.AddMapping(Key.End, UserAction.MoveToLastImage);
            adder.AddMapping(Key.L, UserAction.ShowLists);
            adder.AddMapping(Key.O, UserAction.ChangeOrder);
            adder.AddMapping(Key.F3, UserAction.Search);
            adder.AddMapping(Key.OemQuestion, UserAction.Search);
            adder.AddMapping((int) Key.F, InputType.KeyboardAlt, UserAction.Search);
            adder.AddMapping(Key.Back, UserAction.ClearSearch);
            adder.AddMapping(Key.NumPad0, UserAction.ClearRatingFilter);
            adder.AddMapping(Key.NumPad1, UserAction.ShowRating1OrGreater);
            adder.AddMapping(Key.NumPad2, UserAction.ShowRating2OrGreater);
            adder.AddMapping(Key.NumPad3, UserAction.ShowRating3OrGreater);
            adder.AddMapping(Key.NumPad4, UserAction.ShowRating4OrGreater);
            adder.AddMapping(Key.NumPad5, UserAction.ShowRating5OrGreater);
            adder.AddMapping((int) Key.OemTilde, InputType.KeyboardCtrl, UserAction.ClearRatingFilter);
            adder.AddMapping((int) Key.D1, InputType.KeyboardCtrl, UserAction.ShowRating1OrGreater);
            adder.AddMapping((int) Key.D2, InputType.KeyboardCtrl, UserAction.ShowRating2OrGreater);
            adder.AddMapping((int) Key.D3, InputType.KeyboardCtrl, UserAction.ShowRating3OrGreater);
            adder.AddMapping((int) Key.D4, InputType.KeyboardCtrl, UserAction.ShowRating4OrGreater);
            adder.AddMapping((int) Key.D5, InputType.KeyboardCtrl, UserAction.ShowRating5OrGreater);
            adder.AddMapping(Key.Add, UserAction.ShowOnlyUnrated);
            adder.AddMapping(Key.Subtract, UserAction.ShowOnlyUntagged);
            adder.AddMapping((int) Key.T, InputType.KeyboardAlt, UserAction.TagRenameOrDelete);
            adder.AddMapping((int) Key.Return, InputType.KeyboardAlt, UserAction.ToggleFullscreen);
            adder.AddMapping(Key.Space, UserAction.PlayStopSlideshow);
            adder.AddMapping((int) Key.C, InputType.KeyboardCtrl, UserAction.CopyFiles);
        }
    }
}
