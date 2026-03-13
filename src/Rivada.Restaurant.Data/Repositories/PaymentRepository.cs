using System;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories
{
    public class PaymentRepository : RepositoryBase
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public PaymentRepository(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public int CreatePayment(Payment payment)
        {
            const string sql = @"
INSERT INTO Payments (BillId, PaymentType, Amount, PaidByUserId, PaidAt, CreatedAt, UpdatedAt)
VALUES (@BillId, @PaymentType, @Amount, @PaidByUserId, @PaidAt, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
SELECT last_insert_rowid();";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@BillId", payment.BillId);
            AddParameter(command, "@PaymentType", (int)payment.PaymentType);
            AddParameter(command, "@Amount", payment.Amount);
            AddParameter(command, "@PaidByUserId", payment.PaidByUserId);
            AddParameter(command, "@PaidAt", payment.PaidAt == default ? DateTime.UtcNow : payment.PaidAt);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public bool CreatePaymentAndCloseBill(Payment payment, int closedByUserId)
        {
            var operatorUserId = closedByUserId > 0 ? closedByUserId : payment.PaidByUserId;
            var paidAmount = CreatePaymentAndCloseBillAtomic(payment.BillId, operatorUserId, payment.PaymentType);
            return paidAmount.HasValue;
        }

        public decimal? CreatePaymentAndCloseBillAtomic(int billId, int paidByUserId, PaymentType paymentType)
        {
            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var billState = GetOpenBillState(connection, transaction, billId);
                if (billState == null)
                {
                    transaction.Rollback();
                    return null;
                }

                if (billState.Value.TotalAmount <= 0)
                {
                    transaction.Rollback();
                    return null;
                }

                using var paymentCommand = connection.CreateCommand();
                paymentCommand.Transaction = transaction;
                paymentCommand.CommandText = @"
INSERT INTO Payments (BillId, PaymentType, Amount, PaidByUserId, PaidAt, CreatedAt, UpdatedAt)
VALUES (@BillId, @PaymentType, @Amount, @PaidByUserId, @PaidAt, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";
                AddParameter(paymentCommand, "@BillId", billId);
                AddParameter(paymentCommand, "@PaymentType", (int)paymentType);
                AddParameter(paymentCommand, "@Amount", billState.Value.TotalAmount);
                AddParameter(paymentCommand, "@PaidByUserId", paidByUserId);
                AddParameter(paymentCommand, "@PaidAt", DateTime.UtcNow);
                paymentCommand.ExecuteNonQuery();

                using var billCommand = connection.CreateCommand();
                billCommand.Transaction = transaction;
                billCommand.CommandText = @"
UPDATE Bills
SET Status = @PaidStatus,
    TotalAmount = @TotalAmount,
    ClosedByUserId = @ClosedByUserId,
    ClosedAt = CURRENT_TIMESTAMP,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @BillId AND Status = @OpenStatus;";
                AddParameter(billCommand, "@PaidStatus", (int)BillStatus.Paid);
                AddParameter(billCommand, "@TotalAmount", billState.Value.TotalAmount);
                AddParameter(billCommand, "@ClosedByUserId", paidByUserId);
                AddParameter(billCommand, "@BillId", billId);
                AddParameter(billCommand, "@OpenStatus", (int)BillStatus.Open);

                if (billCommand.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    return null;
                }

                using var tableCommand = connection.CreateCommand();
                tableCommand.Transaction = transaction;
                tableCommand.CommandText = @"
UPDATE RestaurantTables
SET Status = @Status,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @TableId;";
                AddParameter(tableCommand, "@Status", (int)TableStatus.Empty);
                AddParameter(tableCommand, "@TableId", billState.Value.TableId);
                tableCommand.ExecuteNonQuery();

                transaction.Commit();
                return billState.Value.TotalAmount;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                transaction.Rollback();
                return null;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static (int TableId, decimal TotalAmount)? GetOpenBillState(SqliteConnection connection, SqliteTransaction transaction, int billId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
SELECT b.TableId,
       COALESCE(SUM(bi.LineTotal), 0) AS CalculatedTotal
FROM Bills b
LEFT JOIN BillItems bi ON bi.BillId = b.Id
WHERE b.Id = @BillId
  AND b.Status = @OpenStatus
GROUP BY b.Id, b.TableId
LIMIT 1;";
            AddParameter(command, "@BillId", billId);
            AddParameter(command, "@OpenStatus", (int)BillStatus.Open);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var tableId = reader.GetInt32(0);
            var totalAmount = Convert.ToDecimal(reader.GetValue(1));
            return (tableId, totalAmount);
        }
    }
}
