using System.Globalization;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;
using static Rivada.Restaurant.Data.Repositories.StockRepository;

namespace Rivada.Restaurant.Data.Repositories;

public sealed class RevenueRepository
{
    private readonly SqliteDatabaseHelper _database;
    public RevenueRepository(SqliteDatabaseHelper database) => _database=database;

    public RevenueReport Read(DateTime startUtc,DateTime endUtc,int userId)
    {
        using var connection=_database.CreateConnection();
        connection.Open();
        using var transaction=connection.BeginTransaction(deferred:true);
        using (var access=Command(connection,transaction,@"
SELECT COUNT(*) FROM Users u JOIN Roles r ON r.Id=u.RoleId
WHERE u.Id=@user AND u.IsActive=1 AND r.IsActive=1 AND r.Name='Admin';",("@user",userId)))
            if (Convert.ToInt32(access.ExecuteScalar())!=1)
                throw new InvalidOperationException("Ciro raporunu yalnızca yönetici görüntüleyebilir.");

        // SQLite rounds fractional timestamps to milliseconds. Fetch a small margin, then apply
        // the exact [start,end) UTC bounds in .NET so midnight payments cannot shift days.
        const string period="julianday(p.PaidAt)>=julianday(@start)-1.0/86400 AND julianday(p.PaidAt)<julianday(@end)+1.0/86400";
        var result=new RevenueReport();
        var paidBills=new HashSet<int>();
        using (var command=Command(connection,transaction,$@"
SELECT p.Id,p.BillId,p.PaidAt,t.Name,u.Username,p.PaymentType,p.Amount
FROM Payments p JOIN Bills b ON b.Id=p.BillId
JOIN RestaurantTables t ON t.Id=b.TableId JOIN Users u ON u.Id=p.PaidByUserId
WHERE {period} ORDER BY julianday(p.PaidAt) DESC,p.Id DESC;",("@start",startUtc),("@end",endUtc)))
        using (var reader=command.ExecuteReader())
            while(reader.Read())
            {
                var paidAt=DateTimeOffset.Parse(reader.GetString(2),CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal).UtcDateTime;
                if(paidAt<startUtc || paidAt>=endUtc) continue;
                paidBills.Add(reader.GetInt32(1));
                result.Payments.Add(new RevenuePayment {
                Id=reader.GetInt32(0),BillId=reader.GetInt32(1),
                PaidAt=paidAt,
                TableName=reader.GetString(3),Username=reader.GetString(4),
                PaymentType=(PaymentType)reader.GetInt32(5),Amount=reader.GetDecimal(6)
                });
            }
        result.Payments.Sort((a,b)=> { var time=b.PaidAt.CompareTo(a.PaidAt); return time!=0 ? time : b.Id.CompareTo(a.Id); });

        // Sum historical lines in decimal; never join today's Products.Price or multiply payments by item count.
        var products=new Dictionary<(int,string),ProductRevenue>();
        using (var command=Command(connection,transaction,$@"
SELECT i.ProductId,i.ProductNameSnapshot,i.Quantity,i.LineTotal,i.BillId
FROM BillItems i JOIN Payments p ON p.BillId=i.BillId WHERE {period};",("@start",startUtc),("@end",endUtc)))
        using (var reader=command.ExecuteReader())
            while(reader.Read())
            {
                if(!paidBills.Contains(reader.GetInt32(4))) continue;
                var key=(reader.GetInt32(0),reader.GetString(1));
                if (!products.TryGetValue(key,out var row))
                    products[key]=row=new ProductRevenue { ProductId=key.Item1,ProductName=key.Item2 };
                row.Quantity=checked(row.Quantity+reader.GetInt64(2));
                row.Amount+=reader.GetDecimal(3);
            }
        result.Products=products.Values.OrderByDescending(x=>x.Amount).ThenBy(x=>x.ProductName).ToList();

        var openBills=new HashSet<int>();
        using (var command=Command(connection,transaction,@"
SELECT b.Id,i.LineTotal FROM Bills b LEFT JOIN BillItems i ON i.BillId=b.Id
WHERE b.Status=0 AND NOT EXISTS(SELECT 1 FROM Payments p WHERE p.BillId=b.Id);"))
        using (var reader=command.ExecuteReader())
            while(reader.Read())
            {
                openBills.Add(reader.GetInt32(0));
                if(!reader.IsDBNull(1)) result.OpenBillAmount+=reader.GetDecimal(1);
            }
        result.OpenBillCount=openBills.Count;
        transaction.Commit();
        return result;
    }
}
