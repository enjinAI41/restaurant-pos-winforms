using System.Globalization;

namespace Rivada.Restaurant.Models;

public sealed class NotificationFeed
{
    public List<StockAlert> StockAlerts { get; init; } = new();
    public List<TableNotification> TableEvents { get; init; } = new();
}

public sealed class StockAlert
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public long Available { get; init; }
    public int MinimumQuantity { get; init; }
    public string Status => Available <= 0 ? "Tükendi" : "Kritik stok";
}

public sealed class TableNotification
{
    public long Id { get; init; }
    public DateTime Date { get; init; }
    public string Kind { get; init; } = "";
    public string TableName { get; init; } = "";
    public int BillId { get; init; }
    public decimal? Amount { get; init; }
    public int? PaymentType { get; init; }
    public string Action => Kind switch { "Opened" => "Açıldı", "Closed" => "Kapandı", "Paid" => "Ödendi", _ => Kind };
    public string Message => Kind == "Paid"
        ? $"{TableName} ödendi · {Amount?.ToString("C2", CultureInfo.GetCultureInfo("tr-TR"))} · {(PaymentType == 0 ? "Nakit" : "Kart")}"
        : $"{TableName} {(Kind == "Opened" ? "açıldı" : "kapandı")}";
}
