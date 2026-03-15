using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly BillItemRepository _billItemRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly BillRepository _billRepository;
        private readonly RestaurantTableRepository _tableRepository;

        public PaymentService(
            BillItemRepository billItemRepository,
            PaymentRepository paymentRepository,
            BillRepository billRepository,
            RestaurantTableRepository tableRepository)
        {
            _billItemRepository = billItemRepository ?? throw new ArgumentNullException(nameof(billItemRepository));
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _billRepository = billRepository ?? throw new ArgumentNullException(nameof(billRepository));
            _tableRepository = tableRepository ?? throw new ArgumentNullException(nameof(tableRepository));
        }

        public PaymentViewData GetPaymentViewData(int billId, string tableName)
        {
            if (billId <= 0)
            {
                throw new InvalidOperationException("Gecersiz hesap bilgisi.");
            }

            EnsureOpenBillOrThrow(billId);
            var items = _billItemRepository.GetBillItemsByBillId(billId);
            var subtotal = items.Sum(x => x.LineTotal);
            var safeTableName = string.IsNullOrWhiteSpace(tableName) ? "Bilinmeyen Masa" : tableName.Trim();

            return new PaymentViewData
            {
                BillId = billId,
                TableName = safeTableName,
                Items = items,
                Subtotal = subtotal,
                Total = subtotal,
                ReceiptPreview = BuildReceiptPreview(safeTableName, billId, items, subtotal)
            };
        }

        public PaymentCompletionResult CompletePayment(int billId, int paidByUserId, PaymentType paymentType)
        {
            if (billId <= 0 || paidByUserId <= 0)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = "Ödeme işlemi tamamlanamadı."
                };
            }

            var bill = _billRepository.GetBillById(billId);
            if (bill == null || bill.Status != BillStatus.Open)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = "Aktif adisyon bulunamadı."
                };
            }

            decimal? paidAmount;
            try
            {
                paidAmount = _paymentRepository.CreatePaymentAndCloseBillAtomic(billId, paidByUserId, paymentType);
            }
            catch (InvalidOperationException ex)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            catch (SqliteException ex)
            {
                throw new InvalidOperationException("Odeme kaydi olusturulurken veritabani hatasi olustu.", ex);
            }
            if (!paidAmount.HasValue)
            {
                var hasItems = _billItemRepository.GetBillItemsByBillId(billId).Count > 0;
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = hasItems
                        ? "Ödeme işlemi tamamlanamadı."
                        : "Boş adisyon için ödeme alınamaz."
                };
            }

            return new PaymentCompletionResult
            {
                Success = true,
                Message = $"Ödeme başarıyla tamamlandı. Masa sıfırlandı. Toplam: {paidAmount.Value.ToString("C2", CultureInfo.CurrentCulture)}"
            };
        }

        public PaymentCompletionResult ResetTable(int tableId)
        {
            if (tableId <= 0)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = "Geçerli bir masa bulunamadı."
                };
            }

            var table = _tableRepository.GetTableById(tableId);
            if (table == null || !table.IsActive)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = "Geçerli bir masa bulunamadı."
                };
            }

            if (!_billRepository.ForceResetTable(tableId))
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = "Masa durumu güncellenemedi."
                };
            }

            return new PaymentCompletionResult
            {
                Success = true,
                Message = "Masa sıfırlandı."
            };
        }

        private void EnsureOpenBillOrThrow(int billId)
        {
            var bill = _billRepository.GetBillById(billId);
            if (bill == null || bill.Status != BillStatus.Open)
            {
                throw new InvalidOperationException("Aktif adisyon bulunamadı.");
            }
        }

        private static string BuildReceiptPreview(string tableName, int billId, System.Collections.Generic.List<BillItem> items, decimal total)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Rivada Restaurant");
            sb.AppendLine($"Masa: {tableName}");
            sb.AppendLine($"Hesap: {billId}");
            sb.AppendLine($"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine(new string('-', 32));

            foreach (var item in items)
            {
                sb.AppendLine($"{item.ProductNameSnapshot} x{item.Quantity}  {item.LineTotal.ToString("C2", CultureInfo.CurrentCulture)}");
                if (!string.IsNullOrWhiteSpace(item.Note))
                {
                    sb.AppendLine($"  Not: {item.Note}");
                }
            }

            sb.AppendLine(new string('-', 32));
            sb.AppendLine($"TOPLAM: {total.ToString("C2", CultureInfo.CurrentCulture)}");
            return sb.ToString();
        }
    }
}

