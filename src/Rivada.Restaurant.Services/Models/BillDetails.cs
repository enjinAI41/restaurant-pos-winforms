using System.Collections.Generic;
using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Services.Models
{
    public class BillDetails
    {
        public List<BillItem> Items { get; set; } = new List<BillItem>();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
