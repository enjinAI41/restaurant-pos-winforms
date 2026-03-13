using System;

namespace Rivada.Restaurant.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public int OpenedByUserId { get; set; }
        public int? ClosedByUserId { get; set; }
        public BillStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
