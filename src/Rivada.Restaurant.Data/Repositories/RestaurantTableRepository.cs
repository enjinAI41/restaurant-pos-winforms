using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories
{
    public class RestaurantTableRepository : RepositoryBase
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public RestaurantTableRepository(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public List<RestaurantTable> GetAllTables(bool onlyActive = true)
        {
            const string activeSql = @"
SELECT Id, Name, Area, Status, IsActive, CreatedAt, UpdatedAt
FROM RestaurantTables
WHERE IsActive = 1
ORDER BY
CASE Name
    WHEN 'T1' THEN 1
    WHEN 'T2' THEN 2
    WHEN 'T3' THEN 3
    WHEN 'T4' THEN 4
    WHEN 'T5' THEN 5
    WHEN 'İÇ 1' THEN 6
    WHEN 'İÇ 2' THEN 7
    WHEN 'İÇ 3' THEN 8
    WHEN 'İÇ 4' THEN 9
    WHEN 'İÇ 5' THEN 10
    WHEN 'İÇ 6' THEN 11
    WHEN 'İÇ 7' THEN 12
    WHEN 'İÇ 8' THEN 13
    ELSE 999
END,
Id;";

            const string allSql = @"
SELECT Id, Name, Area, Status, IsActive, CreatedAt, UpdatedAt
FROM RestaurantTables
ORDER BY
CASE Name
    WHEN 'T1' THEN 1
    WHEN 'T2' THEN 2
    WHEN 'T3' THEN 3
    WHEN 'T4' THEN 4
    WHEN 'T5' THEN 5
    WHEN 'İÇ 1' THEN 6
    WHEN 'İÇ 2' THEN 7
    WHEN 'İÇ 3' THEN 8
    WHEN 'İÇ 4' THEN 9
    WHEN 'İÇ 5' THEN 10
    WHEN 'İÇ 6' THEN 11
    WHEN 'İÇ 7' THEN 12
    WHEN 'İÇ 8' THEN 13
    ELSE 999
END,
Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = onlyActive ? activeSql : allSql;

            using var reader = command.ExecuteReader();
            var tables = new List<RestaurantTable>();
            while (reader.Read())
            {
                tables.Add(MapTable(reader));
            }

            return tables;
        }

        public RestaurantTable? GetTableById(int tableId)
        {
            const string sql = @"
SELECT Id, Name, Area, Status, IsActive, CreatedAt, UpdatedAt
FROM RestaurantTables
WHERE Id = @Id
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", tableId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapTable(reader);
        }

        public bool UpdateTableStatus(int tableId, int status)
        {
            const string sql = @"
UPDATE RestaurantTables
SET Status = @Status,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Status", status);
            AddParameter(command, "@Id", tableId);

            return command.ExecuteNonQuery() > 0;
        }

        private static RestaurantTable MapTable(SqliteDataReader reader)
        {
            return new RestaurantTable
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Area = reader.IsDBNull(2) ? null : reader.GetString(2),
                Status = (TableStatus)reader.GetInt32(3),
                IsActive = ToBool(reader.GetValue(4)),
                CreatedAt = ToDateTime(reader.GetValue(5)),
                UpdatedAt = ToDateTime(reader.GetValue(6))
            };
        }
    }
}
