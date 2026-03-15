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
SELECT u.Id, u.RoleId, r.Name, u.Username, u.PasswordHash, u.FullName, u.IsActive, u.CreatedAt, u.UpdatedAt
FROM Users u
INNER JOIN Roles r ON r.Id = u.RoleId
WHERE u.Username = @Username AND u.PasswordHash = @PasswordHash AND u.IsActive = 1
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
                RoleName = reader.GetString(2),
                Username = reader.GetString(3),
                PasswordHash = reader.GetString(4),
                FullName = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsActive = ToBool(reader.GetValue(6)),
                CreatedAt = ToDateTime(reader.GetValue(7)),
                UpdatedAt = ToDateTime(reader.GetValue(8))
            };
        }
    }
}
