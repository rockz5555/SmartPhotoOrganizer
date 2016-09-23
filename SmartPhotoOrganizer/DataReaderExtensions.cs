using System;
using System.Data;

namespace SmartPhotoOrganizer
{
    public static class DataReaderExtensions
    {
        public static int GetInt32(this IDataReader reader, string fieldName)
        {
            var fieldIndex = reader.GetOrdinal(fieldName);
            return reader.GetInt32(fieldIndex);
        }

        public static string GetString(this IDataReader reader, string fieldName)
        {
            var fieldIndex = reader.GetOrdinal(fieldName);
            return reader.GetString(fieldIndex);
        }

        public static bool GetBoolean(this IDataReader reader, string fieldName)
        {
            var fieldIndex = reader.GetOrdinal(fieldName);
            return reader.GetBoolean(fieldIndex);
        }

        public static DateTime GetDateTime(this IDataReader reader, string fieldName)
        {
            var fieldIndex = reader.GetOrdinal(fieldName);
            return reader.GetDateTime(fieldIndex);
        }

        public static bool IsDbNull(this IDataReader reader, string fieldName)
        {
            var fieldIndex = reader.GetOrdinal(fieldName);
            return reader.IsDBNull(fieldIndex);
        }
    }
}