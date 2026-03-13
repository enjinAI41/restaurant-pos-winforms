namespace Rivada.Restaurant.Services.Models
{
    public class TableDisplayItem
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public int? OpenBillId { get; set; }
        public decimal CurrentTotalAmount { get; set; }
    }
}
