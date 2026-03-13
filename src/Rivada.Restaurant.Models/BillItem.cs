using System;

namespace Rivada.Restaurant.Models
{
    public class BillItem
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public int ProductId { get; set; }
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
