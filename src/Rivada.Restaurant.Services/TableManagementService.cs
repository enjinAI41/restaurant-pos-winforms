using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services
{
    public class TableManagementService : ITableManagementService
    {
        private readonly RestaurantTableRepository _tableRepository;
        private readonly BillRepository _billRepository;

        public TableManagementService(RestaurantTableRepository tableRepository, BillRepository billRepository)
        {
            _tableRepository = tableRepository ?? throw new ArgumentNullException(nameof(tableRepository));
            _billRepository = billRepository ?? throw new ArgumentNullException(nameof(billRepository));
        }

        public List<TableDisplayItem> GetTableDisplayItems()
        {
            var tables = _tableRepository.GetAllTables();
            var openBillsByTable = _billRepository.GetAllOpenBillsByTableId();
            var items = new List<TableDisplayItem>(tables.Count);

            foreach (var table in tables)
            {
                openBillsByTable.TryGetValue(table.Id, out var openBill);
                var isOccupied = openBill != null;
                var expectedStatus = isOccupied ? TableStatus.Occupied : TableStatus.Empty;
                if (table.Status != expectedStatus)
                {
                    _tableRepository.UpdateTableStatus(table.Id, (int)expectedStatus);
                }

                items.Add(new TableDisplayItem
                {
                    TableId = table.Id,
                    TableName = table.Name,
                    IsOccupied = isOccupied,
                    OpenBillId = openBill?.Id,
                    CurrentTotalAmount = openBill?.TotalAmount ?? 0
                });
            }

            return items;
        }

        public int GetOrCreateOpenBillId(int tableId, int userId)
        {
            if (tableId <= 0)
            {
                throw new InvalidOperationException("Geçerli bir masa bulunamadı.");
            }

            if (userId <= 0)
            {
                throw new InvalidOperationException("Gecersiz kullanici oturumu.");
            }

            var table = _tableRepository.GetTableById(tableId);
            if (table == null || !table.IsActive)
            {
                throw new InvalidOperationException("Geçerli bir masa bulunamadı.");
            }

            var openBill = _billRepository.GetOpenBillByTableId(tableId);
            if (openBill != null)
            {
                return openBill.Id;
            }

            try
            {
                return _billRepository.CreateBill(tableId, userId);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                var existingOpenBill = _billRepository.GetOpenBillByTableId(tableId);
                if (existingOpenBill != null)
                {
                    return existingOpenBill.Id;
                }

                throw new InvalidOperationException("Bu masa için zaten açık bir adisyon var.", ex);
            }
        }
    }
}

