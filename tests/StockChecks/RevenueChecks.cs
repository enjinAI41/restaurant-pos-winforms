using System.Globalization;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services;
using Rivada.Restaurant.WinForms.Forms;

internal static partial class Program
{
    private static readonly DateTime ReportDay=new(2026,9,26);
    private static RevenueService Revenue(Fixture f) => new(new RevenueRepository(f.Db),
        TimeZoneInfo.CreateCustomTimeZone("Test UTC+3",TimeSpan.FromHours(3),"Test UTC+3","Test UTC+3"));

    private static void SetPaidAt(Fixture f,int bill,string date)
    {
        using var c=f.Db.CreateConnection(); c.Open(); using var cmd=c.CreateCommand();
        cmd.CommandText="UPDATE Payments SET PaidAt=@date WHERE BillId=@bill";
        cmd.Parameters.AddWithValue("@date",date); cmd.Parameters.AddWithValue("@bill",bill); cmd.ExecuteNonQuery();
    }

    private static void Pay(Fixture f,int bill,PaymentType method,string date="2026-09-26 09:00:00")
    {
        True(f.Payments.CreatePaymentAndCloseBillAtomic(bill,f.Admin,method).HasValue);
        SetPaidAt(f,bill,date);
    }

    private static void RunRevenueChecks()
    {
        Check("Ciro: açık, sıfırlanan veya ödemesiz kapatılan hesap tahsilata eklenmez",()=> {
            using var f=new Fixture(); var service=Revenue(f);
            var empty=service.GetReport(ReportDay,ReportDay,f.Admin);
            Equal(0m,empty.Total); Equal(0m,empty.Average); Equal(0,empty.PaymentCount);
            f.Order.AddProductToBill(f.Bill,f.Product); f.Order.AddProductToBill(f.Bill,f.Other);
            f.Order.AddProductToBill(f.Bill,f.Other);
            var open=service.GetReport(ReportDay.AddYears(-1),ReportDay.AddYears(-1),f.Admin);
            Equal(0m,open.Total); Equal(360m,open.OpenBillAmount); Equal(1,open.OpenBillCount);
            True(f.Bills.ForceResetTable(f.Table)); Equal(0,service.GetReport(ReportDay,ReportDay,f.Admin).OpenBillCount);
            var closed=f.Bills.CreateBill(f.Table,f.Admin); f.Order.AddProductToBill(closed,f.Product);
            True(f.Bills.CloseBill(closed,f.Admin));
            var report=service.GetReport(ReportDay,ReportDay,f.Admin);
            Equal(0m,report.Total); Equal(0,report.OpenBillCount); Equal(0,report.Products.Count);
        });
        Check("Ciro: çok ürünlü ödemeler çoğalmaz; nakit, kart, adet ve günlük toplamlar tutar",()=> {
            using var f=new Fixture();
            f.Order.AddProductToBill(f.Bill,f.Product); f.Order.AddProductToBill(f.Bill,f.Product);
            f.Order.AddProductToBill(f.Bill,f.Other); Pay(f,f.Bill,PaymentType.Cash);
            var second=f.Bills.CreateBill(f.OtherTable,f.Admin); f.Order.AddProductToBill(second,f.Other);
            Pay(f,second,PaymentType.Card,"2026-09-26 10:00:00");
            var before=f.Snapshot("Products","Categories","Users","RestaurantTables","Bills","BillItems","Payments","PosStockMovements","PosTableNotifications");
            var report=Revenue(f).GetReport(ReportDay,ReportDay,f.Admin);
            Equal(480m,report.Total); Equal(360m,report.Cash); Equal(120m,report.Card);
            Equal(2,report.PaymentCount); Equal(240m,report.Average); Equal(1,report.Days.Count);
            Equal(report.Total,report.Days.Single().Total); Equal(report.Total,report.Products.Sum(x=>x.Amount));
            True(!report.ProductTotalMismatch); Equal(2L,report.Products.Single(x=>x.ProductId==f.Product).Quantity);
            Equal(2L,report.Products.Single(x=>x.ProductId==f.Other).Quantity);
            Equal(second,report.Payments[0].BillId);
            Equal(before,f.Snapshot("Products","Categories","Users","RestaurantTables","Bills","BillItems","Payments","PosStockMovements","PosTableNotifications"));
        });
        Check("Ciro: yerel gece yarısı sınırı ve farklı tarih biçimleri mikrosaniye düzeyinde doğru",()=> {
            using var f=new Fixture(); var bill=f.Bill;
            var dates=new[] {"2026-09-25 20:59:59.9999999","2026-09-25T21:00:00Z","2026-09-26 20:59:59.9999999","2026-09-27T00:00:00+03:00"};
            foreach(var date in dates) {
                f.Order.AddProductToBill(bill,f.Product); Pay(f,bill,PaymentType.Cash,date);
                bill=f.Bills.CreateBill(f.Table,f.Admin);
            }
            var report=Revenue(f).GetReport(ReportDay,ReportDay,f.Admin);
            Equal(2,report.PaymentCount); Equal(240m,report.Total); Equal(2L,report.Products.Single().Quantity);
            Equal(23,report.Payments[0].PaidAt.Hour); Equal(0,report.Payments[1].PaidAt.Hour);
            var range=Revenue(f).GetReport(ReportDay.AddDays(-1),ReportDay.AddDays(1),f.Admin);
            Equal(4,range.PaymentCount); Equal(3,range.Days.Count); Equal(480m,range.Total);
            Equal(2,range.Days.Single(x=>x.Date==ReportDay).PaymentCount);
        });
        Check("Ciro: eski fiyat/ad korunur, pasife alınan ürünün geçmiş satışı raporda kalır",()=> {
            using var f=new Fixture(); f.Order.AddProductToBill(f.Bill,f.Product); Pay(f,f.Bill,PaymentType.Card);
            f.Exec("UPDATE Products SET Name='Yeni İsim',Price=999,IsActive=0 WHERE Id="+f.Product+"; UPDATE Categories SET IsActive=0;");
            var report=Revenue(f).GetReport(ReportDay,ReportDay,f.Admin);
            Equal(120m,report.Total); Equal("Cola",report.Products.Single().ProductName); Equal(120m,report.Products.Single().Amount);
        });
        Check("Ciro: başarısız ve tekrarlanan ödeme toplamı değiştirmez",()=> {
            using var f=new Fixture(); f.Order.AddProductToBill(f.Bill,f.Product);
            f.Exec("CREATE TRIGGER FailRevenuePayment BEFORE UPDATE OF Status ON Bills WHEN NEW.Status=1 BEGIN SELECT RAISE(ABORT,'Test failure'); END;");
            True(!f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Card).HasValue);
            Equal(0m,Revenue(f).GetReport(ReportDay,ReportDay,f.Admin).Total);
            f.Exec("DROP TRIGGER FailRevenuePayment;"); Pay(f,f.Bill,PaymentType.Card);
            True(!f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Cash).HasValue);
            Equal(120m,Revenue(f).GetReport(ReportDay,ReportDay,f.Admin).Total);
        });
        Check("Ciro: kuruşlu tutarlar ve bedelsiz ödemeler doğru hesaplanır",()=> {
            using var f=new Fixture();
            f.Exec("UPDATE Products SET Price=10.10 WHERE Id="+f.Product+"; UPDATE Products SET Price=0.20 WHERE Id="+f.Other);
            for(var i=0;i<3;i++)f.Order.AddProductToBill(f.Bill,f.Product);
            f.Order.AddProductToBill(f.Bill,f.Other); Pay(f,f.Bill,PaymentType.Cash);
            var free=f.Bills.CreateBill(f.Table,f.Admin); f.Exec("UPDATE Products SET Price=0 WHERE Id="+f.Other);
            f.Order.AddProductToBill(free,f.Other); Pay(f,free,PaymentType.Card);
            var report=Revenue(f).GetReport(ReportDay,ReportDay,f.Admin);
            Equal(30.50m,report.Total); Equal(30.50m,report.Products.Sum(x=>x.Amount));
            Equal(2,report.PaymentCount); Equal(15.25m,report.Average); True(!report.ProductTotalMismatch);
        });
        Check("Ciro: eski veride ürün/ödeme farkı görünür, gerçek ödeme tutarı esas alınır",()=> {
            using var f=new Fixture(); f.Order.AddProductToBill(f.Bill,f.Product); Pay(f,f.Bill,PaymentType.Cash);
            f.Exec("UPDATE Payments SET Amount=100 WHERE BillId="+f.Bill);
            var report=Revenue(f).GetReport(ReportDay,ReportDay,f.Admin);
            Equal(100m,report.Total); Equal(120m,report.Products.Sum(x=>x.Amount)); True(report.ProductTotalMismatch);
        });
        Check("Ciro: yetkisiz/pasif kullanıcı ve ters tarih aralığı reddedilir",()=> {
            using var f=new Fixture(); var service=Revenue(f);
            Throws(()=>service.GetReport(ReportDay,ReportDay,f.Waiter));
            Throws(()=>service.GetReport(ReportDay,ReportDay,0));
            Throws(()=>service.GetReport(ReportDay,ReportDay.AddDays(-1),f.Admin));
            f.Exec("UPDATE Users SET IsActive=0 WHERE Id="+f.Admin);
            Throws(()=>service.GetReport(ReportDay,ReportDay,f.Admin));
        });
    }

    private static void PreviewRevenue()
    {
        ApplicationConfiguration.Initialize();
        using var f=new Fixture(preserveDefaults:true);
        var food=(int)f.Scalar("SELECT Id FROM Products WHERE Name='Bonfile'");
        var bill=f.Bill;
        for(var i=0;i<12;i++)
        {
            for(var q=0;q<=i%3;q++) f.Order.AddProductToBill(bill,f.Product);
            if(i%3==0) f.Order.AddProductToBill(bill,food);
            if(i%2==1) f.Order.AddProductToBill(bill,f.Other);
            var local=DateTime.Today.AddDays(-(i/4)).AddHours(12).AddMinutes(i*11);
            Pay(f,bill,i%2==0 ? PaymentType.Cash : PaymentType.Card,
                TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local,DateTimeKind.Unspecified)).ToString("yyyy-MM-dd HH:mm:ss.fffffff",CultureInfo.InvariantCulture));
            bill=f.Bills.CreateBill(f.Table,f.Admin);
        }
        f.Order.AddProductToBill(bill,food); f.Order.AddProductToBill(bill,f.Product);
        Application.Run(new RevenueForm(new User { Id=f.Admin,RoleName="Admin",Username="admin" },new RevenueService(new RevenueRepository(f.Db))));
    }
}
