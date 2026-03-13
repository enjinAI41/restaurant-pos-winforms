using System;
using System.Globalization;
using System.Linq;
using System.Text;
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

        public PaymentService(BillItemRepository billItemRepository, PaymentRepository paymentRepository)
        {
            _billItemRepository = billItemRepository ?? throw new ArgumentNullException(nameof(billItemRepository));
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public PaymentViewData GetPaymentViewData(int billId, string tableName)
        {
            var items = _billItemRepository.GetBillItemsByBillId(billId);
            var subtotal = items.Sum(x => x.LineTotal);

            return new PaymentViewData
            {
                BillId = billId,
                TableName = tableName,
                Items = items,
                Subtotal = subtotal,
                Total = subtotal,
                ReceiptPreview = BuildReceiptPreview(tableName, billId, items, subtotal)
            };
        }

        public PaymentCompletionResult CompletePayment(int billId, int paidByUserId, PaymentType paymentType)
        {
            if (billId <= 0 || paidByUserId <= 0)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = "Gecersiz odeme baglami. Hesabi yeniden acip tekrar deneyin."
                };
            }

            var paidAmount = _paymentRepository.CreatePaymentAndCloseBillAtomic(billId, paidByUserId, paymentType);
            if (!paidAmount.HasValue)
            {
                var hasItems = _billItemRepository.GetBillItemsByBillId(billId).Count > 0;
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = hasItems
                        ? "Odeme tamamlanamadi. Hesap zaten kapatilmis olabilir."
                        : "Hesapta urun yok. Odemeden once urun ekleyin."
                };
            }

            return new PaymentCompletionResult
            {
                Success = true,
                Message = $"Odeme basariyla tamamlandi. Toplam: {paidAmount.Value.ToString("C2", CultureInfo.CurrentCulture)}"
            };
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
