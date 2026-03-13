using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Data.Repositories
{
    public class BillItemRepository : RepositoryBase
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public BillItemRepository(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public int AddBillItem(BillItem item)
        {
            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
INSERT INTO BillItems
(BillId, ProductId, ProductNameSnapshot, Quantity, UnitPrice, LineTotal, Note, CreatedAt, UpdatedAt)
VALUES
(@BillId, @ProductId, @ProductNameSnapshot, @Quantity, @UnitPrice, @LineTotal, @Note, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
SELECT last_insert_rowid();";
                AddParameter(insertCommand, "@BillId", item.BillId);
                AddParameter(insertCommand, "@ProductId", item.ProductId);
                AddParameter(insertCommand, "@ProductNameSnapshot", item.ProductNameSnapshot);
                AddParameter(insertCommand, "@Quantity", item.Quantity);
                AddParameter(insertCommand, "@UnitPrice", item.UnitPrice);
                AddParameter(insertCommand, "@LineTotal", item.LineTotal);
                AddParameter(insertCommand, "@Note", item.Note);

                var billItemId = Convert.ToInt32(insertCommand.ExecuteScalar());

                UpdateBillTotalInternal(connection, transaction, item.BillId);
                transaction.Commit();

                return billItemId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public BillItem? GetBillItemByProductAndNote(int billId, int productId, string? note)
        {
            const string sql = @"
SELECT Id, BillId, ProductId, ProductNameSnapshot, Quantity, UnitPrice, LineTotal, Note, CreatedAt, UpdatedAt
FROM BillItems
WHERE BillId = @BillId
  AND ProductId = @ProductId
  AND COALESCE(Note, '') = COALESCE(@Note, '')
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@BillId", billId);
            AddParameter(command, "@ProductId", productId);
            AddParameter(command, "@Note", note);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new BillItem
            {
                Id = reader.GetInt32(0),
                BillId = reader.GetInt32(1),
                ProductId = reader.GetInt32(2),
                ProductNameSnapshot = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = Convert.ToDecimal(reader.GetValue(5)),
                LineTotal = Convert.ToDecimal(reader.GetValue(6)),
                Note = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt = ToDateTime(reader.GetValue(8)),
                UpdatedAt = ToDateTime(reader.GetValue(9))
            };
        }

        public BillItem? GetBillItemById(int billItemId)
        {
            const string sql = @"
SELECT Id, BillId, ProductId, ProductNameSnapshot, Quantity, UnitPrice, LineTotal, Note, CreatedAt, UpdatedAt
FROM BillItems
WHERE Id = @Id
LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", billItemId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new BillItem
            {
                Id = reader.GetInt32(0),
                BillId = reader.GetInt32(1),
                ProductId = reader.GetInt32(2),
                ProductNameSnapshot = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = Convert.ToDecimal(reader.GetValue(5)),
                LineTotal = Convert.ToDecimal(reader.GetValue(6)),
                Note = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt = ToDateTime(reader.GetValue(8)),
                UpdatedAt = ToDateTime(reader.GetValue(9))
            };
        }

        public bool UpdateBillItemQuantity(int billItemId, int quantity)
        {
            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var billId = GetBillIdByBillItemId(connection, transaction, billItemId);
                if (billId == null)
                {
                    transaction.Rollback();
                    return false;
                }

                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
UPDATE BillItems
SET Quantity = @Quantity,
    LineTotal = UnitPrice * @Quantity,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";
                AddParameter(updateCommand, "@Quantity", quantity);
                AddParameter(updateCommand, "@Id", billItemId);

                var affected = updateCommand.ExecuteNonQuery();
                if (affected == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                UpdateBillTotalInternal(connection, transaction, billId.Value);
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool RemoveBillItem(int billItemId)
        {
            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var billId = GetBillIdByBillItemId(connection, transaction, billItemId);
                if (billId == null)
                {
                    transaction.Rollback();
                    return false;
                }

                using var deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM BillItems WHERE Id = @Id;";
                AddParameter(deleteCommand, "@Id", billItemId);

                var affected = deleteCommand.ExecuteNonQuery();
                if (affected == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                UpdateBillTotalInternal(connection, transaction, billId.Value);
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool UpdateBillItemNote(int billItemId, string? note)
        {
            const string sql = @"
UPDATE BillItems
SET Note = @Note,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", billItemId);
            AddParameter(command, "@Note", string.IsNullOrWhiteSpace(note) ? null : note.Trim());

            return command.ExecuteNonQuery() > 0;
        }

        public List<BillItem> GetBillItemsByBillId(int billId)
        {
            const string sql = @"
SELECT Id, BillId, ProductId, ProductNameSnapshot, Quantity, UnitPrice, LineTotal, Note, CreatedAt, UpdatedAt
FROM BillItems
WHERE BillId = @BillId
ORDER BY Id;";

            using var connection = _databaseHelper.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@BillId", billId);

            using var reader = command.ExecuteReader();
            var items = new List<BillItem>();
            while (reader.Read())
            {
                items.Add(new BillItem
                {
                    Id = reader.GetInt32(0),
                    BillId = reader.GetInt32(1),
                    ProductId = reader.GetInt32(2),
                    ProductNameSnapshot = reader.GetString(3),
                    Quantity = reader.GetInt32(4),
                    UnitPrice = Convert.ToDecimal(reader.GetValue(5)),
                    LineTotal = Convert.ToDecimal(reader.GetValue(6)),
                    Note = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreatedAt = ToDateTime(reader.GetValue(8)),
                    UpdatedAt = ToDateTime(reader.GetValue(9))
                });
            }

            return items;
        }

        private static int? GetBillIdByBillItemId(SqliteConnection connection, SqliteTransaction transaction, int billItemId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT BillId FROM BillItems WHERE Id = @Id LIMIT 1;";
            AddParameter(command, "@Id", billItemId);

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(result);
        }

        private static void UpdateBillTotalInternal(SqliteConnection connection, SqliteTransaction transaction, int billId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
UPDATE Bills
SET TotalAmount = COALESCE((SELECT SUM(LineTotal) FROM BillItems WHERE BillId = @BillId), 0),
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Id = @BillId;";
            AddParameter(command, "@BillId", billId);
            command.ExecuteNonQuery();
        }
    }
}
