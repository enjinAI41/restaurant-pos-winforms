using Microsoft.Data.Sqlite;

namespace Rivada.Restaurant.Data.Database;

internal static class StockMigration
{
    public static void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS PosStockBalances (
    ProductId INTEGER PRIMARY KEY REFERENCES Products(Id) ON DELETE RESTRICT,
    IsTracked INTEGER NOT NULL DEFAULT 0 CHECK(IsTracked IN (0,1)),
    Quantity INTEGER NOT NULL DEFAULT 0 CHECK(Quantity BETWEEN 0 AND 2000000000),
    MinimumQuantity INTEGER NOT NULL DEFAULT 0 CHECK(MinimumQuantity BETWEEN 0 AND 2000000000),
    Revision INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS PosStockMovements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE RESTRICT,
    ProductName TEXT NOT NULL,
    Kind TEXT NOT NULL CHECK(Kind IN ('Giriş','Fire / Çıkış','Sayım','Satış','Ayar')),
    QuantityChange INTEGER NOT NULL,
    BalanceAfter INTEGER NOT NULL CHECK(BalanceAfter >= 0),
    Note TEXT NOT NULL,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE RESTRICT,
    BillId INTEGER REFERENCES Bills(Id) ON DELETE RESTRICT,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS IX_PosStockMovements_Product ON PosStockMovements(ProductId, Id DESC);
CREATE UNIQUE INDEX IF NOT EXISTS UX_PosStockMovements_Sale ON PosStockMovements(BillId, ProductId) WHERE Kind = 'Satış';";
        command.ExecuteNonQuery();
    }
}
