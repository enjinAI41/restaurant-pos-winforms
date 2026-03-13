using System.Collections.Generic;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Services.Models
{
    public class PaymentViewData
    {
        public int BillId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public List<BillItem> Items { get; set; } = new List<BillItem>();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string ReceiptPreview { get; set; } = string.Empty;
    }
}
