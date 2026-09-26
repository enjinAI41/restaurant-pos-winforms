namespace Rivada.Restaurant.Models;

public sealed class RevenueReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RefreshedAt { get; set; }
    public List<RevenuePayment> Payments { get; } = new();
    public List<DailyRevenue> Days { get; set; } = new();
    public List<ProductRevenue> Products { get; set; } = new();
    public int OpenBillCount { get; set; }
    public decimal OpenBillAmount { get; set; }
    public decimal Total => Payments.Sum(x=>x.Amount);
    public decimal Cash => Payments.Where(x=>x.PaymentType==PaymentType.Cash).Sum(x=>x.Amount);
    public decimal Card => Payments.Where(x=>x.PaymentType==PaymentType.Card).Sum(x=>x.Amount);
    public int PaymentCount => Payments.Count;
    public decimal Average => PaymentCount==0 ? 0 : Total/PaymentCount;
    public bool ProductTotalMismatch => Math.Abs(Products.Sum(x=>x.Amount)-Total)>0.005m;
}

public sealed class RevenuePayment
{
    public int Id { get; init; }
    public int BillId { get; init; }
    public DateTime PaidAt { get; set; }
    public string TableName { get; init; } = "";
    public string Username { get; init; } = "";
    public PaymentType PaymentType { get; init; }
    public string Method => PaymentType==PaymentType.Cash ? "Nakit" : "Kart";
    public decimal Amount { get; init; }
}

public sealed class DailyRevenue
{
    public DateTime Date { get; init; }
    public int PaymentCount { get; init; }
    public decimal Cash { get; init; }
    public decimal Card { get; init; }
    public decimal Total => Cash+Card;
}

public sealed class ProductRevenue
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public long Quantity { get; set; }
    public decimal Amount { get; set; }
}
