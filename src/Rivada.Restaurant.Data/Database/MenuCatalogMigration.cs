using System.Text.Json;
using Microsoft.Data.Sqlite;
using static Rivada.Restaurant.Data.Repositories.StockRepository;

namespace Rivada.Restaurant.Data.Database;

internal static class MenuCatalogMigration
{
    private const string MigrationId = "20260926-rivada-menu-stock99-v1";

    public static bool IsPending(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        using var table = Command(connection, transaction,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='PosAppMigrations';");
        if (Convert.ToInt64(table.ExecuteScalar()) == 0) return true;
        using var applied = Command(connection, transaction,
            "SELECT COUNT(*) FROM PosAppMigrations WHERE Id=@id;", ("@id", MigrationId));
        return Convert.ToInt64(applied.ExecuteScalar()) == 0;
    }

    public static void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        using (var schema = Command(connection, transaction, @"
CREATE TABLE IF NOT EXISTS PosAppMigrations (
    Id TEXT PRIMARY KEY,
    AppliedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);")) schema.ExecuteNonQuery();
        if (!IsPending(connection, transaction)) return;

        // A counted balance must cover the quantities already reserved on open bills.
        using (var reserved = Command(connection, transaction, @"
SELECT p.Name FROM BillItems i JOIN Bills b ON b.Id=i.BillId JOIN Products p ON p.Id=i.ProductId
WHERE b.Status=0 GROUP BY i.ProductId HAVING SUM(i.Quantity)>99 LIMIT 1;"))
        {
            if (reserved.ExecuteScalar() is string name)
                throw new InvalidOperationException($"{name}: açık adisyonlarda 99 adetten fazla ürün var. Menü/stok güncellemesinden önce ilgili adisyonları kapatın.");
        }

        using var resource = typeof(MenuCatalogMigration).Assembly.GetManifestResourceStream("Rivada.Menu.2026-09-26.json")
            ?? throw new InvalidOperationException("Menü verisi bulunamadı.");
        using var catalog = JsonDocument.Parse(resource);
        var items = catalog.RootElement.GetProperty("Items").EnumerateArray().ToArray();
        if (items.Length != 86) throw new InvalidOperationException("Menü verisi eksik.");

        foreach (var item in items)
        {
            var category = item.GetProperty("Category").GetString()!;
            var name = item.GetProperty("Name").GetString()!;
            var price = item.GetProperty("Price").GetDecimal();
            using (var categoryUpdate = Command(connection, transaction, @"
INSERT INTO Categories(Name,IsActive) VALUES(@name,1)
ON CONFLICT(Name) DO UPDATE SET IsActive=1,UpdatedAt=CURRENT_TIMESTAMP;", ("@name", category)))
                categoryUpdate.ExecuteNonQuery();
            using var categoryQuery = Command(connection, transaction, "SELECT Id FROM Categories WHERE Name=@name;", ("@name", category));
            var categoryId = Convert.ToInt32(categoryQuery.ExecuteScalar());
            var productId = FindProduct(connection, transaction, categoryId, name);

            if (productId.HasValue)
            {
                // Preserve identity and all old bill snapshots when names/categories change.
                using var update = Command(connection, transaction, @"
UPDATE Products SET CategoryId=@category,Name=@name,Price=@price,IsActive=1,UpdatedAt=CURRENT_TIMESTAMP WHERE Id=@id;",
                    ("@category", categoryId), ("@name", name), ("@price", price), ("@id", productId.Value));
                update.ExecuteNonQuery();
            }
            else
            {
                using var insert = Command(connection, transaction,
                    "INSERT INTO Products(CategoryId,Name,Price,IsActive) VALUES(@category,@name,@price,1);",
                    ("@category", categoryId), ("@name", name), ("@price", price));
                insert.ExecuteNonQuery();
            }
        }

        using var user = Command(connection, transaction, @"
SELECT u.Id FROM Users u JOIN Roles r ON r.Id=u.RoleId
ORDER BY CASE WHEN r.Name='Admin' THEN 0 ELSE 1 END,u.IsActive DESC,u.Id LIMIT 1;");
        var userId = user.ExecuteScalar() ?? throw new InvalidOperationException("Stok başlangıcı için kullanıcı kaydı bulunamadı.");
        using (var audit = Command(connection, transaction, @"
INSERT INTO PosStockMovements(ProductId,ProductName,Kind,QuantityChange,BalanceAfter,Note,UserId)
SELECT p.Id,p.Name,'Sayım',99-COALESCE(s.Quantity,0),99,
    '1.3 menü güncellemesi: tüm ürünlerde takip açık, başlangıç 99 adet',@user
FROM Products p LEFT JOIN PosStockBalances s ON s.ProductId=p.Id;", ("@user", userId))) audit.ExecuteNonQuery();
        using (var stock = Command(connection, transaction, @"
INSERT INTO PosStockBalances(ProductId,IsTracked,Quantity,Revision)
SELECT Id,1,99,1 FROM Products WHERE 1
ON CONFLICT(ProductId) DO UPDATE SET IsTracked=1,Quantity=99,Revision=PosStockBalances.Revision+1;")) stock.ExecuteNonQuery();
        using var mark = Command(connection, transaction, "INSERT INTO PosAppMigrations(Id) VALUES(@id);", ("@id", MigrationId));
        mark.ExecuteNonQuery();
    }

    private static int? FindProduct(SqliteConnection connection, SqliteTransaction transaction, int categoryId, string name)
    {
        using var exact = Command(connection, transaction,
            "SELECT Id FROM Products WHERE CategoryId=@category AND Name=@name;", ("@category", categoryId), ("@name", name));
        if (exact.ExecuteScalar() is long id) return checked((int)id);

        var alias = name switch
        {
            "Sac Kavurma" => "Saç Kavurma",
            "Tavuk Sac Kavurma" => "Tavuk Saç Kavurma",
            "Şalgam" => "Şalgam (30cl)",
            "Su (500 ml)" => "Su (0.5 lt)",
            "Su (1,5 lt)" => "Su (1.5 lt)",
            "Red Bull" => "Redbull",
            _ => name
        };
        using var query = Command(connection, transaction,
            "SELECT Id FROM Products WHERE Name=@name OR Name=@alias;", ("@name", name), ("@alias", alias));
        using var reader = query.ExecuteReader();
        if (!reader.Read()) return null;
        var candidate = reader.GetInt32(0);
        // Ambiguous old names remain untouched; add an unambiguous catalog entry instead.
        return reader.Read() ? null : candidate;
    }
}
