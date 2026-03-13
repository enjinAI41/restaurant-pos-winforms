using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories
{
    public class CategoryRepository : RepositoryBase
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public CategoryRepository(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public List<Category> GetAllCategories(bool onlyActive = true)
        {
            const string activeSql = @"
SELECT Id, Name, IsActive, CreatedAt, UpdatedAt
FROM Categories
WHERE IsActive = 1
ORDER BY Name;";

            const string allSql = @"
SELECT Id, Name, IsActive, CreatedAt, UpdatedAt
FROM Categories
ORDER BY Name;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = onlyActive ? activeSql : allSql;

            using var reader = command.ExecuteReader();
            var categories = new List<Category>();
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    IsActive = ToBool(reader.GetValue(2)),
                    CreatedAt = ToDateTime(reader.GetValue(3)),
                    UpdatedAt = ToDateTime(reader.GetValue(4))
                });
            }

            return categories;
        }
    }
}
