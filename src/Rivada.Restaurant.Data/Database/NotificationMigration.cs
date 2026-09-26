using Microsoft.Data.Sqlite;
using static Rivada.Restaurant.Data.Repositories.StockRepository;

namespace Rivada.Restaurant.Data.Database;

internal static class NotificationMigration
{
    public static bool IsPending(SqliteConnection connection)
    {
        using var command = Command(connection, null,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='PosTableNotifications';");
        return Convert.ToInt32(command.ExecuteScalar()) == 0;
    }

    public static void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        // Snapshots deliberately have no bill FK: resetting a table deletes its open bill.
        // Triggers share the transaction of the actual operation, so failures leave no notification.
        using var command = Command(connection, transaction, @"
CREATE TABLE IF NOT EXISTS PosTableNotifications (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Kind TEXT NOT NULL CHECK(Kind IN ('Opened','Closed','Paid')),
    TableName TEXT NOT NULL,
    BillId INTEGER NOT NULL,
    Amount REAL,
    PaymentType INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TRIGGER IF NOT EXISTS PosNotifyBillOpened AFTER INSERT ON Bills
WHEN NEW.Status=0
BEGIN
    INSERT INTO PosTableNotifications(Kind,TableName,BillId)
    SELECT 'Opened',Name,NEW.Id FROM RestaurantTables WHERE Id=NEW.TableId;
END;
CREATE TRIGGER IF NOT EXISTS PosNotifyBillClosed AFTER UPDATE OF Status ON Bills
WHEN OLD.Status=0 AND NEW.Status=1
BEGIN
    INSERT INTO PosTableNotifications(Kind,TableName,BillId)
    SELECT 'Closed',Name,NEW.Id FROM RestaurantTables WHERE Id=NEW.TableId;
END;
CREATE TRIGGER IF NOT EXISTS PosNotifyBillReset BEFORE DELETE ON Bills
WHEN OLD.Status=0
BEGIN
    INSERT INTO PosTableNotifications(Kind,TableName,BillId)
    SELECT 'Closed',Name,OLD.Id FROM RestaurantTables WHERE Id=OLD.TableId;
END;
CREATE TRIGGER IF NOT EXISTS PosNotifyPayment AFTER INSERT ON Payments
BEGIN
    INSERT INTO PosTableNotifications(Kind,TableName,BillId,Amount,PaymentType)
    SELECT 'Paid',t.Name,NEW.BillId,NEW.Amount,NEW.PaymentType
    FROM Bills b JOIN RestaurantTables t ON t.Id=b.TableId WHERE b.Id=NEW.BillId;
END;");
        command.ExecuteNonQuery();
    }
}
