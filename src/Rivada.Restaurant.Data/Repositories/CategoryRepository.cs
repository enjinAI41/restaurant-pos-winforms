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

        public Category? GetCategoryById(int categoryId)
        {
            const string sql = @"
SELECT Id, Name, IsActive, CreatedAt, UpdatedAt
FROM Categories
WHERE Id = @Id
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", categoryId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                IsActive = ToBool(reader.GetValue(2)),
                CreatedAt = ToDateTime(reader.GetValue(3)),
                UpdatedAt = ToDateTime(reader.GetValue(4))
            };
        }

        public int AddCategory(Category category)
        {
            const string sql = @"
INSERT INTO Categories (Name, IsActive, CreatedAt, UpdatedAt)
VALUES (@Name, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
SELECT last_insert_rowid();";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Name", category.Name);
            AddParameter(command, "@IsActive", category.IsActive ? 1 : 0);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public bool UpdateCategory(Category category)
        {
            const string sql = @"
UPDATE Categories
SET Name = @Name,
    IsActive = @IsActive,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", category.Id);
            AddParameter(command, "@Name", category.Name);
            AddParameter(command, "@IsActive", category.IsActive ? 1 : 0);

            return command.ExecuteNonQuery() > 0;
        }

        public bool SetCategoryActive(int categoryId, bool isActive)
        {
            const string sql = @"
UPDATE Categories
SET IsActive = @IsActive,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", categoryId);
            AddParameter(command, "@IsActive", isActive ? 1 : 0);

            return command.ExecuteNonQuery() > 0;
        }
    }
}
