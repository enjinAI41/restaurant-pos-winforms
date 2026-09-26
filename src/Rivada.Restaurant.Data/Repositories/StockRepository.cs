using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories;

public sealed class StockRepository : RepositoryBase
{
    private readonly SqliteDatabaseHelper _database;
    public StockRepository(SqliteDatabaseHelper database) => _database = database;

    public List<StockItem> GetStock()
    {
        using var connection = _database.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Id, p.Name, c.Name, p.IsActive, COALESCE(s.IsTracked,0), COALESCE(s.Quantity,0),
       CASE WHEN s.IsTracked=1 THEN COALESCE(r.Reserved,0) ELSE 0 END,
       COALESCE(s.MinimumQuantity,0), COALESCE(s.Revision,0)
FROM Products p JOIN Categories c ON c.Id=p.CategoryId
LEFT JOIN PosStockBalances s ON s.ProductId=p.Id
LEFT JOIN (SELECT i.ProductId, SUM(i.Quantity) Reserved FROM BillItems i
    JOIN Bills b ON b.Id=i.BillId WHERE b.Status=0 GROUP BY i.ProductId) r ON r.ProductId=p.Id
ORDER BY c.Name, p.Name;";
        using var reader = command.ExecuteReader();
        var result = new List<StockItem>();
        while (reader.Read()) result.Add(new StockItem {
            ProductId=reader.GetInt32(0), ProductName=reader.GetString(1), CategoryName=reader.GetString(2),
            IsActive=reader.GetBoolean(3), IsTracked=reader.GetBoolean(4), Quantity=reader.GetInt32(5),
            Reserved=reader.GetInt64(6), MinimumQuantity=reader.GetInt32(7), Revision=reader.GetInt64(8)
        });
        return result;
    }

    public List<StockMovement> GetHistory(int productId)
    {
        using var connection = _database.CreateConnection();
        connection.Open();
        using var command = Command(connection, null, @"
SELECT m.Id, m.CreatedAt, m.ProductName, m.Kind, m.QuantityChange, m.BalanceAfter, m.Note, u.Username, m.BillId
FROM PosStockMovements m JOIN Users u ON u.Id=m.UserId
WHERE m.ProductId=@id ORDER BY m.Id DESC LIMIT 1000;", ("@id", productId));
        using var reader = command.ExecuteReader();
        var result = new List<StockMovement>();
        while (reader.Read()) result.Add(new StockMovement {
            Id=reader.GetInt64(0), Date=DateTime.SpecifyKind(ToDateTime(reader.GetValue(1)), DateTimeKind.Utc).ToLocalTime(),
            ProductName=reader.GetString(2), Kind=reader.GetString(3), QuantityChange=reader.GetInt64(4),
            BalanceAfter=reader.GetInt64(5), Note=reader.GetString(6), Username=reader.GetString(7),
            BillId=reader.IsDBNull(8) ? null : reader.GetInt32(8)
        });
        return result;
    }

    public void Configure(int productId, bool tracked, int minimum, long expectedRevision, int userId)
    {
        if (minimum < 0 || minimum > 2000000000) throw new InvalidOperationException("Geçersiz kritik stok adedi.");
        using var connection = _database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        EnsureAdmin(connection, transaction, userId);
        using (var create = Command(connection, transaction,
            "INSERT OR IGNORE INTO PosStockBalances(ProductId) SELECT Id FROM Products WHERE Id=@id;", ("@id", productId))) create.ExecuteNonQuery();
        var state = ReadBalance(connection, transaction, productId);
        if (state.Revision != expectedRevision) throw new InvalidOperationException("Stok değişti. Listeyi yenileyip tekrar deneyin.");
        if (state.Tracked != tracked && Reserved(connection, transaction, productId) > 0)
            throw new InvalidOperationException("Bu ürün açık adisyonda bulunuyor. Takibi değiştirmeden önce adisyonları kapatın veya ürünü adisyondan çıkarın.");
        using (var update = Command(connection, transaction,
            "UPDATE PosStockBalances SET IsTracked=@tracked, MinimumQuantity=@min, Revision=Revision+1 WHERE ProductId=@id;",
            ("@tracked", tracked ? 1 : 0), ("@min", minimum), ("@id", productId))) update.ExecuteNonQuery();
        Record(connection, transaction, productId, "Ayar", 0, state.Quantity,
            $"Takip: {(tracked ? "açık" : "kapalı")}; kritik adet: {minimum}", userId, null);
        transaction.Commit();
    }

    public void Adjust(int productId, string kind, int quantity, string note, long expectedRevision, int userId)
    {
        if (kind != "Giriş" && kind != "Fire / Çıkış" && kind != "Sayım") throw new InvalidOperationException("Geçersiz stok işlemi.");
        if (quantity < 0 || (kind != "Sayım" && quantity == 0)) throw new InvalidOperationException("İşlem için geçerli bir adet girin.");
        var movementNote = string.IsNullOrWhiteSpace(note) ? kind : note.Trim();
        if (movementNote.Length > 250) throw new InvalidOperationException("Açıklama en fazla 250 karakter olabilir.");
        using var connection = _database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        EnsureAdmin(connection, transaction, userId);
        var state = ReadBalance(connection, transaction, productId);
        if (!state.Tracked) throw new InvalidOperationException("Önce bu ürün için stok takibini açın.");
        if (state.Revision != expectedRevision) throw new InvalidOperationException("Stok değişti. Listeyi yenileyip tekrar deneyin.");
        long delta = kind == "Sayım" ? (long)quantity - state.Quantity : kind == "Giriş" ? quantity : -(long)quantity;
        long balance = state.Quantity + delta;
        if (balance < 0 || balance > 2000000000) throw new InvalidOperationException("Stok adedi 0–2.000.000.000 aralığında olmalıdır.");
        if (balance < Reserved(connection, transaction, productId)) throw new InvalidOperationException("Stok, açık adisyonlara ayrılmış adetten az olamaz. Önce ilgili adisyonları kontrol edin.");
        using (var update = Command(connection, transaction,
            "UPDATE PosStockBalances SET Quantity=@qty, Revision=Revision+1 WHERE ProductId=@id;", ("@qty", balance), ("@id", productId))) update.ExecuteNonQuery();
        Record(connection, transaction, productId, kind, delta, balance, movementNote, userId, null);
        transaction.Commit();
    }

    internal static void EnsureAvailable(SqliteConnection connection, SqliteTransaction transaction, int productId, int additional)
    {
        using var command = Command(connection, transaction, @"
SELECT p.Name, s.Quantity FROM PosStockBalances s JOIN Products p ON p.Id=s.ProductId
WHERE s.ProductId=@id AND s.IsTracked=1;", ("@id", productId));
        string name; long quantity;
        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read()) return;
            name = reader.GetString(0); quantity = reader.GetInt64(1);
        }
        long available = quantity - Reserved(connection, transaction, productId);
        if (additional > available) throw new InvalidOperationException($"{name}: yetersiz stok. Satılabilir adet: {Math.Max(0, available)}.");
    }

    internal static void CompleteSale(SqliteConnection connection, SqliteTransaction transaction, int billId, int userId)
    {
        using var command = Command(connection, transaction, @"
SELECT i.ProductId, SUM(i.Quantity) FROM BillItems i
JOIN PosStockBalances s ON s.ProductId=i.ProductId AND s.IsTracked=1
WHERE i.BillId=@bill GROUP BY i.ProductId;", ("@bill", billId));
        var items = new List<(int ProductId, long Quantity)>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) items.Add((reader.GetInt32(0), reader.GetInt64(1)));
        foreach (var item in items)
        {
            var state = ReadBalance(connection, transaction, item.ProductId);
            if (state.Quantity < Reserved(connection, transaction, item.ProductId))
                throw new InvalidOperationException("Stok yetersiz. Ödeme öncesinde stokları kontrol edin.");
            long balance = state.Quantity - item.Quantity;
            using (var update = Command(connection, transaction,
                "UPDATE PosStockBalances SET Quantity=@qty, Revision=Revision+1 WHERE ProductId=@id;",
                ("@qty", balance), ("@id", item.ProductId))) update.ExecuteNonQuery();
            Record(connection, transaction, item.ProductId, "Satış", -item.Quantity, balance, $"Adisyon #{billId}", userId, billId);
        }
    }

    private static (bool Tracked, long Quantity, long Revision) ReadBalance(SqliteConnection connection, SqliteTransaction transaction, int productId)
    {
        using var command = Command(connection, transaction,
            "SELECT IsTracked, Quantity, Revision FROM PosStockBalances WHERE ProductId=@id;", ("@id", productId));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Stok kaydı bulunamadı. Önce stok takibini açın.");
        return (reader.GetBoolean(0), reader.GetInt64(1), reader.GetInt64(2));
    }

    private static long Reserved(SqliteConnection connection, SqliteTransaction transaction, int productId)
    {
        using var command = Command(connection, transaction, @"
SELECT COALESCE(SUM(i.Quantity),0) FROM BillItems i JOIN Bills b ON b.Id=i.BillId
WHERE b.Status=0 AND i.ProductId=@id;", ("@id", productId));
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static void EnsureAdmin(SqliteConnection connection, SqliteTransaction transaction, int userId)
    {
        using var command = Command(connection, transaction,
            "SELECT COUNT(*) FROM Users u JOIN Roles r ON r.Id=u.RoleId WHERE u.Id=@id AND u.IsActive=1 AND r.Name='Admin';", ("@id", userId));
        if (Convert.ToInt32(command.ExecuteScalar()) != 1) throw new InvalidOperationException("Stok değiştirmek için yönetici yetkisi gereklidir.");
    }

    private static void Record(SqliteConnection connection, SqliteTransaction transaction, int productId, string kind,
        long delta, long balance, string note, int userId, int? billId)
    {
        using var command = Command(connection, transaction, @"
INSERT INTO PosStockMovements(ProductId,ProductName,Kind,QuantityChange,BalanceAfter,Note,UserId,BillId)
SELECT Id,Name,@kind,@delta,@balance,@note,@user,@bill FROM Products WHERE Id=@id;",
            ("@kind", kind), ("@delta", delta), ("@balance", balance), ("@note", note), ("@user", userId), ("@bill", billId), ("@id", productId));
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Ürün bulunamadı.");
    }

    internal static SqliteCommand Command(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string, object?)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (key, value) in parameters) command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        return command;
    }
}
