using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories
{
    public class BillRepository : RepositoryBase
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public BillRepository(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public int CreateBill(int tableId, int openedByUserId)
        {
            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
INSERT INTO Bills (TableId, OpenedByUserId, Status, TotalAmount, OpenedAt, CreatedAt, UpdatedAt)
VALUES (@TableId, @OpenedByUserId, @Status, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
SELECT last_insert_rowid();";
                AddParameter(insertCommand, "@TableId", tableId);
                AddParameter(insertCommand, "@OpenedByUserId", openedByUserId);
                AddParameter(insertCommand, "@Status", (int)BillStatus.Open);

                var billId = Convert.ToInt32(insertCommand.ExecuteScalar());

                using var tableCommand = connection.CreateCommand();
                tableCommand.Transaction = transaction;
                tableCommand.CommandText = @"
UPDATE RestaurantTables
SET Status = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @TableId;";
                AddParameter(tableCommand, "@TableId", tableId);

                if (tableCommand.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("Hesap olusturulurken masa bulunamadi.");
                }

                transaction.Commit();
                return billId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public Bill? GetOpenBillByTableId(int tableId)
        {
            const string sql = @"
SELECT Id, TableId, OpenedByUserId, ClosedByUserId, Status, TotalAmount, OpenedAt, ClosedAt, CreatedAt, UpdatedAt
FROM Bills
WHERE TableId = @TableId AND Status = @Status
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@TableId", tableId);
            AddParameter(command, "@Status", (int)BillStatus.Open);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapBill(reader);
        }

        public Bill? GetBillById(int billId)
        {
            const string sql = @"
SELECT Id, TableId, OpenedByUserId, ClosedByUserId, Status, TotalAmount, OpenedAt, ClosedAt, CreatedAt, UpdatedAt
FROM Bills
WHERE Id = @BillId
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@BillId", billId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapBill(reader);
        }

        public Dictionary<int, Bill> GetAllOpenBillsByTableId()
        {
            const string sql = @"
SELECT Id, TableId, OpenedByUserId, ClosedByUserId, Status, TotalAmount, OpenedAt, ClosedAt, CreatedAt, UpdatedAt
FROM Bills
WHERE Status = @Status;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Status", (int)BillStatus.Open);

            using var reader = command.ExecuteReader();
            var openBills = new Dictionary<int, Bill>();
            while (reader.Read())
            {
                var bill = MapBill(reader);
                openBills[bill.TableId] = bill;
            }

            return openBills;
        }

        public bool UpdateBillTotal(int billId, decimal totalAmount)
        {
            const string sql = @"
UPDATE Bills
SET TotalAmount = @TotalAmount,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", billId);
            AddParameter(command, "@TotalAmount", totalAmount);

            return command.ExecuteNonQuery() > 0;
        }

        public bool CloseBill(int billId, int closedByUserId)
        {
            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var tableId = GetTableIdForBill(connection, transaction, billId);
                if (tableId == null)
                {
                    transaction.Rollback();
                    return false;
                }

                using var closeCommand = connection.CreateCommand();
                closeCommand.Transaction = transaction;
                closeCommand.CommandText = @"
UPDATE Bills
SET Status = @PaidStatus,
    ClosedByUserId = @ClosedByUserId,
    ClosedAt = CURRENT_TIMESTAMP,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @BillId AND Status = @OpenStatus;";
                AddParameter(closeCommand, "@PaidStatus", (int)BillStatus.Paid);
                AddParameter(closeCommand, "@ClosedByUserId", closedByUserId);
                AddParameter(closeCommand, "@BillId", billId);
                AddParameter(closeCommand, "@OpenStatus", (int)BillStatus.Open);

                var affectedRows = closeCommand.ExecuteNonQuery();
                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                using var updateTableCommand = connection.CreateCommand();
                updateTableCommand.Transaction = transaction;
                updateTableCommand.CommandText = @"
UPDATE RestaurantTables
SET Status = 0,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @TableId;";
                AddParameter(updateTableCommand, "@TableId", tableId.Value);
                updateTableCommand.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool ForceResetTable(int tableId)
        {
            if (tableId <= 0)
            {
                return false;
            }

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var deleteOpenBills = connection.CreateCommand();
                deleteOpenBills.Transaction = transaction;
                deleteOpenBills.CommandText = @"
DELETE FROM Bills
WHERE TableId = @TableId
  AND Status = @OpenStatus;";
                AddParameter(deleteOpenBills, "@TableId", tableId);
                AddParameter(deleteOpenBills, "@OpenStatus", (int)BillStatus.Open);
                deleteOpenBills.ExecuteNonQuery();

                using var updateTable = connection.CreateCommand();
                updateTable.Transaction = transaction;
                updateTable.CommandText = @"
UPDATE RestaurantTables
SET Status = @Status,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @TableId;";
                AddParameter(updateTable, "@Status", (int)TableStatus.Empty);
                AddParameter(updateTable, "@TableId", tableId);
                if (updateTable.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static int? GetTableIdForBill(SqliteConnection connection, SqliteTransaction transaction, int billId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT TableId FROM Bills WHERE Id = @BillId LIMIT 1;";
            AddParameter(command, "@BillId", billId);

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(result);
        }

        private static Bill MapBill(SqliteDataReader reader)
        {
            return new Bill
            {
                Id = reader.GetInt32(0),
                TableId = reader.GetInt32(1),
                OpenedByUserId = reader.GetInt32(2),
                ClosedByUserId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Status = (BillStatus)reader.GetInt32(4),
                TotalAmount = Convert.ToDecimal(reader.GetValue(5)),
                OpenedAt = ToDateTime(reader.GetValue(6)),
                ClosedAt = ToNullableDateTime(reader.GetValue(7)),
                CreatedAt = ToDateTime(reader.GetValue(8)),
                UpdatedAt = ToDateTime(reader.GetValue(9))
            };
        }
    }
}
