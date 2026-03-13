using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Rivada.Restaurant.Data.Database
{
    public class SqliteDatabaseHelper
    {
        private readonly string _connectionString;

        public SqliteDatabaseHelper(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Veritabani yolu bos olamaz.", nameof(databasePath));
            }

            var fullPath = Path.GetFullPath(databasePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = fullPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true
            }.ToString();
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}
