using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Services;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.WinForms
{
    internal sealed class AppServices
    {
        public required IAuthService AuthService { get; init; }
        public required ITableManagementService TableManagementService { get; init; }
        public required IOrderService OrderService { get; init; }
        public required IPaymentService PaymentService { get; init; }
        public required IProductManagementService ProductManagementService { get; init; }
    }

    internal static class AppComposition
    {
        public static AppServices Create()
        {
            var databasePath = Path.Combine(AppContext.BaseDirectory, "restaurant.db");
            var dbHelper = new SqliteDatabaseHelper(databasePath);
            var initializer = new DatabaseInitializer(dbHelper);
            initializer.Initialize();

            ValidateDatabaseState(dbHelper);

            var userRepository = new UserRepository(dbHelper);
            var tableRepository = new RestaurantTableRepository(dbHelper);
            var billRepository = new BillRepository(dbHelper);
            var categoryRepository = new CategoryRepository(dbHelper);
            var productRepository = new ProductRepository(dbHelper);
            var billItemRepository = new BillItemRepository(dbHelper);
            var paymentRepository = new PaymentRepository(dbHelper);

            return new AppServices
            {
                AuthService = new AuthService(userRepository),
                TableManagementService = new TableManagementService(tableRepository, billRepository),
                OrderService = new OrderService(categoryRepository, productRepository, billItemRepository),
                PaymentService = new PaymentService(billItemRepository, paymentRepository),
                ProductManagementService = new ProductManagementService(productRepository, categoryRepository)
            };
        }

        private static void ValidateDatabaseState(SqliteDatabaseHelper dbHelper)
        {
            using var connection = dbHelper.CreateConnection();
            connection.Open();

            if (!TableExists(connection, "Users"))
            {
                throw new InvalidOperationException("Veritabani baslatma hatasi: Users tablosu bulunamadi.");
            }

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Users;";
            var userCount = Convert.ToInt32(command.ExecuteScalar());
            if (userCount <= 0)
            {
                throw new InvalidOperationException("Veritabani baslatma hatasi: Users tablosunda kullanici yok.");
            }
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @TableName;";
            command.Parameters.AddWithValue("@TableName", tableName);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
    }
}
