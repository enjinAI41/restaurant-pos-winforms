using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;
using static Rivada.Restaurant.Data.Repositories.StockRepository;

namespace Rivada.Restaurant.Data.Repositories;

public sealed class NotificationRepository : RepositoryBase
{
    private readonly SqliteDatabaseHelper _database;
    public NotificationRepository(SqliteDatabaseHelper database) => _database = database;

    public NotificationFeed GetFeed()
    {
        using var connection = _database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: true);
        var result = new NotificationFeed();
        using (var stock = Command(connection, transaction, @"
SELECT p.Id,p.Name,s.Quantity-COALESCE(r.Reserved,0) Available,s.MinimumQuantity
FROM Products p JOIN Categories c ON c.Id=p.CategoryId
JOIN PosStockBalances s ON s.ProductId=p.Id
LEFT JOIN (SELECT i.ProductId,SUM(i.Quantity) Reserved FROM BillItems i
    JOIN Bills b ON b.Id=i.BillId WHERE b.Status=0 GROUP BY i.ProductId) r ON r.ProductId=p.Id
WHERE p.IsActive=1 AND c.IsActive=1 AND s.IsTracked=1
    AND s.Quantity-COALESCE(r.Reserved,0)<=s.MinimumQuantity
ORDER BY Available,p.Name;"))
        using (var reader = stock.ExecuteReader())
            while (reader.Read()) result.StockAlerts.Add(new StockAlert {
                ProductId=reader.GetInt32(0), ProductName=reader.GetString(1),
                Available=reader.GetInt64(2), MinimumQuantity=reader.GetInt32(3)
            });
        using (var events = Command(connection, transaction, @"
SELECT Id,CreatedAt,Kind,TableName,BillId,Amount,PaymentType
FROM PosTableNotifications ORDER BY Id DESC LIMIT 100;"))
        using (var reader = events.ExecuteReader())
            while (reader.Read()) result.TableEvents.Add(new TableNotification {
                Id=reader.GetInt64(0), Date=DateTime.SpecifyKind(ToDateTime(reader.GetValue(1)),DateTimeKind.Utc).ToLocalTime(),
                Kind=reader.GetString(2), TableName=reader.GetString(3), BillId=reader.GetInt32(4),
                Amount=reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                PaymentType=reader.IsDBNull(6) ? null : reader.GetInt32(6)
            });
        transaction.Commit();
        return result;
    }
}
