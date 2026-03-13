using System;

namespace Rivada.Restaurant.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public int PaidByUserId { get; set; }
        public DateTime PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
