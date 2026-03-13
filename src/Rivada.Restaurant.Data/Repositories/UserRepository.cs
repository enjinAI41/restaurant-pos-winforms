using System;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories
{
    public class UserRepository : RepositoryBase
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public UserRepository(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public User? GetUserByUsernameAndPassword(string username, string password)
        {
            const string sql = @"
SELECT Id, RoleId, Username, PasswordHash, FullName, IsActive, CreatedAt, UpdatedAt
FROM Users
WHERE Username = @Username AND PasswordHash = @PasswordHash AND IsActive = 1
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Username", username);
            AddParameter(command, "@PasswordHash", password);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapUser(reader);
        }

        private static User MapUser(SqliteDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(0),
                RoleId = reader.GetInt32(1),
                Username = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                FullName = reader.IsDBNull(4) ? null : reader.GetString(4),
                IsActive = ToBool(reader.GetValue(5)),
                CreatedAt = ToDateTime(reader.GetValue(6)),
                UpdatedAt = ToDateTime(reader.GetValue(7))
            };
        }
    }
}
