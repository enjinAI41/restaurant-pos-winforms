using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories
{
    public class ProductRepository : RepositoryBase
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public ProductRepository(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public List<Product> GetProductsByCategory(int categoryId, bool onlyActive = true)
        {
            const string activeSql = @"
SELECT Id, CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt
FROM Products
WHERE CategoryId = @CategoryId AND IsActive = 1
ORDER BY Name;";

            const string allSql = @"
SELECT Id, CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt
FROM Products
WHERE CategoryId = @CategoryId
ORDER BY Name;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = onlyActive ? activeSql : allSql;
            AddParameter(command, "@CategoryId", categoryId);

            using var reader = command.ExecuteReader();
            var products = new List<Product>();
            while (reader.Read())
            {
                products.Add(MapProduct(reader));
            }

            return products;
        }

        public List<Product> GetAllProducts(bool onlyActive = false)
        {
            const string activeSql = @"
SELECT Id, CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt
FROM Products
WHERE IsActive = 1
ORDER BY Name;";

            const string allSql = @"
SELECT Id, CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt
FROM Products
ORDER BY Name;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = onlyActive ? activeSql : allSql;

            using var reader = command.ExecuteReader();
            var products = new List<Product>();
            while (reader.Read())
            {
                products.Add(MapProduct(reader));
            }

            return products;
        }

        public Product? GetProductById(int productId)
        {
            const string sql = @"
SELECT Id, CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt
FROM Products
WHERE Id = @Id
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", productId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapProduct(reader);
        }

        public int AddProduct(Product product)
        {
            const string sql = @"
INSERT INTO Products (CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt)
VALUES (@CategoryId, @Name, @Price, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
SELECT last_insert_rowid();";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@CategoryId", product.CategoryId);
            AddParameter(command, "@Name", product.Name);
            AddParameter(command, "@Price", product.Price);
            AddParameter(command, "@IsActive", product.IsActive ? 1 : 0);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public bool UpdateProduct(Product product)
        {
            const string sql = @"
UPDATE Products
SET CategoryId = @CategoryId,
    Name = @Name,
    Price = @Price,
    IsActive = @IsActive,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", product.Id);
            AddParameter(command, "@CategoryId", product.CategoryId);
            AddParameter(command, "@Name", product.Name);
            AddParameter(command, "@Price", product.Price);
            AddParameter(command, "@IsActive", product.IsActive ? 1 : 0);

            return command.ExecuteNonQuery() > 0;
        }

        public bool SetProductInactive(int productId)
        {
            const string sql = @"
UPDATE Products
SET IsActive = 0,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", productId);

            return command.ExecuteNonQuery() > 0;
        }

        public bool DeleteProduct(int productId)
        {
            const string sql = "DELETE FROM Products WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", productId);

            return command.ExecuteNonQuery() > 0;
        }

        private static Product MapProduct(SqliteDataReader reader)
        {
            return new Product
            {
                Id = reader.GetInt32(0),
                CategoryId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Price = Convert.ToDecimal(reader.GetValue(3)),
                IsActive = ToBool(reader.GetValue(4)),
                CreatedAt = ToDateTime(reader.GetValue(5)),
                UpdatedAt = ToDateTime(reader.GetValue(6))
            };
        }
    }
}
