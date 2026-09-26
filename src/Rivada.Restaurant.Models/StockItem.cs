namespace Rivada.Restaurant.Models;

public sealed class StockItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsTracked { get; set; }
    public int Quantity { get; set; }
    public long Reserved { get; set; }
    public long Available => Quantity - Reserved;
    public int MinimumQuantity { get; set; }
    public long Revision { get; set; }
    public string Status => !IsTracked ? "Takip kapalı" : Available <= 0 ? "Tükendi" : Available <= MinimumQuantity ? "Kritik" : "Yeterli";
}

public sealed class StockMovement
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string ProductName { get; set; } = "";
    public string Kind { get; set; } = "";
    public long QuantityChange { get; set; }
    public long BalanceAfter { get; set; }
    public string Note { get; set; } = "";
    public string Username { get; set; } = "";
    public int? BillId { get; set; }
}
