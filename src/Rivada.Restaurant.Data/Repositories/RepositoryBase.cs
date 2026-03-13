using System;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Rivada.Restaurant.Data.Repositories
{
    public abstract class RepositoryBase
    {
        protected static bool ToBool(object value)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
        }

        protected static DateTime ToDateTime(object value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture);
        }

        protected static DateTime? ToNullableDateTime(object value)
        {
            if (value == DBNull.Value || value is null)
            {
                return null;
            }

            return ToDateTime(value);
        }

        protected static void AddParameter(SqliteCommand command, string name, object? value)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }
}
