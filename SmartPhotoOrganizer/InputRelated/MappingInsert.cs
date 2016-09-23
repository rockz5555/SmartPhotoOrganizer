using System.Data;
using System.Data.SQLite;
using System.Windows.Input;

namespace SmartPhotoOrganizer.InputRelated
{
    public class MappingInsert
    {
        private readonly SQLiteCommand _insertMapper;
        private readonly SQLiteParameter _inputCodeParam;
        private readonly SQLiteParameter _inputTypeParam;
        private readonly SQLiteParameter _actionCodeParam;

        public MappingInsert(SQLiteConnection connection)
        {
            _insertMapper = new SQLiteCommand("INSERT INTO inputMapping (inputCode, inputType, actionCode) VALUES (?, ?, ?)", connection);
            _inputCodeParam = _insertMapper.Parameters.Add("inputCode", DbType.Int32);
            _inputTypeParam = _insertMapper.Parameters.Add("inputType", DbType.Int32);
            _actionCodeParam = _insertMapper.Parameters.Add("actionCode", DbType.Int32);
        }

        public void AddMapping(Key key, UserAction action)
        {
            AddMapping((int)key, InputType.Keyboard, action);
        }

        public void AddMapping(MouseButton mouseButton, UserAction action)
        {
            AddMapping((int)mouseButton, InputType.Mouse, action);
        }

        public void AddMapping(MouseWheelAction wheelAction, UserAction action)
        {
            AddMapping((int)wheelAction, InputType.MouseWheel, action);
        }

        public void AddMapping(int inputCode, InputType inputType, UserAction action)
        {
            _inputCodeParam.Value = inputCode;
            _inputTypeParam.Value = (int)inputType;
            _actionCodeParam.Value = (int)action;

            _insertMapper.ExecuteNonQuery();
        }
    }
}
