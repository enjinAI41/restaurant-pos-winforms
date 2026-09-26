using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Database;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services;
using Rivada.Restaurant.WinForms.Forms;

internal static partial class Program
{
    private static string Root = "";
    private static int Passed;
    [STAThread]
    private static int Main(string[] args)
    {
        Root = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine(Path.GetTempPath(), "RivadaStockChecks", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Root);
        try
        {
            if(args.Contains("--revenue-preview")) { PreviewRevenue(); return 0; }
            if (args.Contains("--notifications-preview"))
            {
                ApplicationConfiguration.Initialize();
                using var f=new Fixture(preserveDefaults:true);
                f.Adjust("Sayım",4);
                f.Stock.Configure(f.Product,true,5,f.Item.Revision,f.Admin);
                f.Bills.CreateBill(f.OtherTable,f.Admin); f.Bills.ForceResetTable(f.OtherTable);
                f.Order.AddProductToBill(f.Bill,f.Other);
                f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Card);
                var other=f.Stock.GetStock().Single(x=>x.ProductId==f.Other);
                f.Stock.Adjust(f.Other,"Sayım",0,"",other.Revision,f.Admin);
                var tableRepository=new RestaurantTableRepository(f.Db);
                var form=new TableManagementForm(new User { Id=f.Admin,RoleName="Admin",Username="admin",FullName="Sistem Yöneticisi" },
                    new TableManagementService(tableRepository,f.Bills),f.Order,
                    new PaymentService(f.Items,f.Payments,f.Bills,tableRepository),
                    new ProductManagementService(f.Products,new CategoryRepository(f.Db)),
                    new StockService(f.Stock),new NotificationService(new NotificationRepository(f.Db)),new RevenueService(new RevenueRepository(f.Db)));
                if(args.Contains("--compact")) form.Size=new System.Drawing.Size(1024,680);
                Application.Run(form);
                return 0;
            }
            if (args.Contains("--preview"))
            {
                ApplicationConfiguration.Initialize();
                using var f = new Fixture(preserveDefaults:true);
                StockManagementForm.ShowForUser(new User { Id=f.Admin, RoleName="Admin", Username="admin" },new StockService(f.Stock));
                return 0;
            }
            Check("Yeni veritabanı: 86 menü kalemi, doğru fiyatlar, tüm stoklar 99 ve takip açık", () => {
                using var f=new Fixture(preserveDefaults:true);
                Equal(86,f.Stock.GetStock().Count);
                True(f.Stock.GetStock().All(x=>x.IsTracked && x.Quantity==99));
                VerifyCatalog(f);
                new DatabaseInitializer(f.Db).Initialize();
                Equal(86L,f.Scalar("SELECT COUNT(*) FROM PosStockMovements"));
                True(!System.IO.Directory.Exists(Path.Combine(f.Directory,"backups")));
            });
            Check("Menü güncellemesi: eski ürün kimlikleri ve satışlar korunur, eksikler eklenir, tüm stoklar bir kez 99 olur", () => {
                using var f=new Fixture(preserveDefaults:true);
                f.Order.AddProductToBill(f.Bill,f.Product);
                var sales=f.Snapshot("Bills","BillItems","Payments");
                var redBull=f.Scalar("SELECT Id FROM Products WHERE Name='Red Bull'");
                var water=f.Scalar("SELECT Id FROM Products WHERE Name='Su (500 ml)'");
                var meat=f.Scalar("SELECT Id FROM Products WHERE Name='Sac Kavurma'");
                f.Exec(@"DELETE FROM PosAppMigrations;
UPDATE Categories SET Name='SOĞUK & SICAK İÇECEKLER',IsActive=0 WHERE Name='İÇECEKLER';
UPDATE Products SET Name='Redbull' WHERE Name='Red Bull';
UPDATE Products SET Name='Su (0.5 lt)' WHERE Name='Su (500 ml)';
UPDATE Products SET Name='Saç Kavurma' WHERE Name='Sac Kavurma';
UPDATE Products SET Price=1,IsActive=0;
UPDATE PosStockBalances SET Quantity=15,MinimumQuantity=3,Revision=8;
DELETE FROM PosStockMovements WHERE ProductId=(SELECT Id FROM Products WHERE Name='Lokum');
DELETE FROM PosStockBalances WHERE ProductId=(SELECT Id FROM Products WHERE Name='Lokum');
DELETE FROM Products WHERE Name='Lokum';
INSERT INTO Products(CategoryId,Name,Price,IsActive) SELECT CategoryId,'Özel Ürün',321,0 FROM Products LIMIT 1;
UPDATE Users SET FullName='Özel Yönetici' WHERE Username='admin';
UPDATE RestaurantTables SET Name='Özel Masa' WHERE Id=(SELECT MIN(Id) FROM RestaurantTables);");
                var people=f.Snapshot("Users","RestaurantTables");
                new DatabaseInitializer(f.Db).Initialize();
                VerifyCatalog(f);
                Equal(87,f.Stock.GetStock().Count);
                True(f.Stock.GetStock().All(x=>x.IsTracked && x.Quantity==99));
                Equal(redBull,f.Scalar("SELECT Id FROM Products WHERE Name='Red Bull'"));
                Equal(water,f.Scalar("SELECT Id FROM Products WHERE Name='Su (500 ml)'"));
                Equal(meat,f.Scalar("SELECT Id FROM Products WHERE Name='Sac Kavurma'"));
                Equal(3,f.Item.MinimumQuantity); Equal(9L,f.Item.Revision);
                Equal(1L,f.Item.Reserved); Equal(98L,f.Item.Available);
                Equal(321L,f.Scalar("SELECT Price FROM Products WHERE Name='Özel Ürün'"));
                Equal(sales,f.Snapshot("Bills","BillItems","Payments"));
                Equal(people,f.Snapshot("Users","RestaurantTables"));
                True(Directory.GetFiles(Path.Combine(f.Directory,"backups"),"*.db").Length==1);
                var count=f.Scalar("SELECT COUNT(*) FROM PosStockMovements");
                f.Adjust("Fire / Çıkış",5);
                f.Exec("UPDATE Products SET Price=777 WHERE Id="+f.Product);
                new DatabaseInitializer(f.Db).Initialize();
                Equal(94,f.Item.Quantity); Equal(777L,f.Scalar("SELECT Price FROM Products WHERE Id="+f.Product));
                Equal(count+1,f.Scalar("SELECT COUNT(*) FROM PosStockMovements"));
                True(Directory.GetFiles(Path.Combine(f.Directory,"backups"),"*.db").Length==1);
            });
            Check("99 üzeri açık rezervasyon: menü ve stok güncellemesi tamamen geri alınır", () => {
                using var f=new Fixture();
                f.Order.AddProductToBill(f.Bill,f.Product);
                f.Exec("DELETE FROM PosAppMigrations; UPDATE Products SET Price=7 WHERE Id="+f.Product+"; UPDATE BillItems SET Quantity=100,LineTotal=UnitPrice*100;");
                var before=f.Snapshot();
                Throws(()=>new DatabaseInitializer(f.Db).Initialize());
                Equal(before,f.Snapshot());
                Equal(0L,f.Scalar("SELECT COUNT(*) FROM PosAppMigrations"));
                Equal(0L,f.Scalar("SELECT COUNT(*) FROM PosStockMovements"));
                f.Exec("UPDATE BillItems SET Quantity=1,LineTotal=UnitPrice;");
                new DatabaseInitializer(f.Db).Initialize();
                Equal(99,f.Item.Quantity); Equal(1L,f.Item.Reserved);
            });
            Check("Eski veritabanı: menü, fiyat, kullanıcı, masa ve açık adisyon korunur; yedek alınır", () => {
                using var f=new Fixture();
                f.Order.AddProductToBill(f.Bill,f.Product);
                f.Exec("DROP TABLE PosStockMovements; DROP TABLE PosStockBalances; UPDATE Products SET Price=777,IsActive=0 WHERE Id="+f.Product);
                f.Exec("UPDATE Categories SET IsActive=0 WHERE Id=(SELECT CategoryId FROM Products WHERE Id="+f.Product+"); UPDATE RestaurantTables SET Name='Özel Masa',IsActive=0 WHERE Id="+f.Table+"; UPDATE Users SET FullName='Özel Kullanıcı' WHERE Id="+f.Admin);
                var baseline=f.Snapshot();
                new DatabaseInitializer(f.Db).Initialize();
                Equal(baseline,f.Snapshot());
                True(Directory.GetFiles(Path.Combine(f.Directory,"backups"),"*.db").Length==1);
                new DatabaseInitializer(f.Db).Initialize();
                Equal(baseline,f.Snapshot());
                True(Directory.GetFiles(Path.Combine(f.Directory,"backups"),"*.db").Length==1);
            });
            Check("Stok giriş, fire ve sayım açıklama gerektirmeden çalışır", () => {
                using var f=new Fixture(); f.Enable(10);
                f.Adjust("Fire / Çıkış",2); Equal(8,f.Item.Quantity);
                f.Adjust("Sayım",6); Equal(6,f.Item.Quantity);
                Throws(()=>f.Adjust("Fire / Çıkış",7));
                f.Stock.Adjust(f.Product,"Giriş",1,"",f.Item.Revision,f.Admin);
                Equal(7,f.Item.Quantity); Equal(5,f.Stock.GetHistory(f.Product).Count);
            });
            Check("Yetkisiz kullanıcı ve eski ekran değeri stok değiştiremez", () => {
                using var f=new Fixture(); f.Enable(5);
                var revision=f.Item.Revision; f.Adjust("Giriş",1);
                Throws(()=>f.Stock.Adjust(f.Product,"Sayım",1,"Eski ekran",revision,f.Admin));
                Throws(()=>f.Stock.Configure(f.Product,false,0,f.Item.Revision,f.Waiter));
                Throws(()=>f.Stock.Adjust(f.Product,"Giriş",1,"Yetkisiz",f.Item.Revision,f.Waiter));
                Equal(6,f.Item.Quantity);
            });
            Check("Sipariş rezervasyonu, yetersiz stok ve geri bırakma", () => {
                using var f=new Fixture(); f.Enable(2);
                f.Order.AddProductToBill(f.Bill,f.Product);
                f.Order.AddProductToBill(f.Bill,f.Product);
                Equal(2,f.Item.Quantity); Equal(2L,f.Item.Reserved); Equal(0L,f.Item.Available);
                Throws(()=>f.Order.AddProductToBill(f.Bill,f.Product));
                var id=f.Items.GetBillItemsByBillId(f.Bill).Single().Id;
                f.Order.DecreaseBillItemQuantity(id); Equal(1L,f.Item.Available);
                f.Order.RemoveBillItem(id); Equal(2L,f.Item.Available);
                Equal(2,f.Stock.GetHistory(f.Product).Count);
            });
            Check("Not birleştirme: stok sıfırken çift rezervasyon oluşmaz", () => {
                using var f=new Fixture(); f.Enable(2);
                f.Order.AddProductToBill(f.Bill,f.Product,"A"); f.Order.AddProductToBill(f.Bill,f.Product,"B");
                var id=f.Items.GetBillItemsByBillId(f.Bill).Single(x=>x.Note=="A").Id;
                f.Order.UpdateBillItemNote(id,"B");
                Equal(1,f.Items.GetBillItemsByBillId(f.Bill).Count); Equal(2L,f.Item.Reserved); Equal(2,f.Item.Quantity);
            });
            Check("Açık adisyonda takip değişikliği ve ayrılan adedin altına sayım engellenir", () => {
                using var f=new Fixture(); f.Enable(3);
                f.Order.AddProductToBill(f.Bill,f.Product);
                Throws(()=>f.Stock.Configure(f.Product,false,0,f.Item.Revision,f.Admin));
                Throws(()=>f.Adjust("Sayım",0));
                f.Order.AddProductToBill(f.Bill,f.Other);
                Throws(()=>f.Stock.Configure(f.Other,true,0,0,f.Admin));
            });
            Check("Masa sıfırlama: ayrılan stok serbest kalır", () => {
                using var f=new Fixture(); f.Enable(4); f.Order.AddProductToBill(f.Bill,f.Product);
                True(f.Bills.ForceResetTable(f.Table)); Equal(0L,f.Item.Reserved); Equal(4,f.Item.Quantity);
            });
            Check("Ödeme: stok bir kez düşer, tekrar ödeme ve ödenmiş kalemde düzenleme engellenir", () => {
                using var f=new Fixture(); f.Enable(4); f.Order.AddProductToBill(f.Bill,f.Product); f.Order.AddProductToBill(f.Bill,f.Product);
                var id=f.Items.GetBillItemsByBillId(f.Bill).Single().Id;
                True(f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Cash).HasValue);
                Equal(2,f.Item.Quantity); Equal(0L,f.Item.Reserved);
                True(!f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Cash).HasValue);
                Equal(1,f.Stock.GetHistory(f.Product).Count(x=>x.Kind=="Satış"));
                Throws(()=>f.Items.AdjustBillItemQuantity(id,1)); Throws(()=>f.Items.RemoveBillItem(id));
                True(!f.Items.UpdateBillItemNote(id,"X")); Equal(2,f.Item.Quantity);
            });
            Check("Ödeme hatası: stok, hareket, ödeme ve adisyon birlikte geri alınır", () => {
                using var f=new Fixture(); f.Enable(4); f.Order.AddProductToBill(f.Bill,f.Product);
                f.Exec("CREATE TRIGGER SimulatedPaymentFailure BEFORE INSERT ON Payments BEGIN SELECT RAISE(ABORT,'Test failure'); END;");
                True(!f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Cash).HasValue);
                Equal(4,f.Item.Quantity); Equal(1L,f.Item.Reserved);
                Equal(0,f.Stock.GetHistory(f.Product).Count(x=>x.Kind=="Satış")); Equal(0L,f.Scalar("SELECT COUNT(*) FROM Payments"));
            });
            Check("İki eşzamanlı sipariş: son ürün yalnızca bir masaya ayrılır", () => {
                using var f=new Fixture(); f.Enable(1);
                var second=f.Bills.CreateBill(f.OtherTable,f.Admin);
                var successes=0;
                Parallel.ForEach(new[] {f.Bill,second}, bill => { try { f.Order.AddProductToBill(bill,f.Product); Interlocked.Increment(ref successes); } catch (InvalidOperationException) {} });
                Equal(1,successes); Equal(1L,f.Item.Reserved); Equal(0L,f.Item.Available);
            });
            Check("Takipsiz ürün ve nakit/kart ödemesi mevcut davranışı korur", () => {
                using var f=new Fixture(); f.Order.AddProductToBill(f.Bill,f.Product);
                True(f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,(PaymentType)1).HasValue);
                Equal(0L,f.Scalar("SELECT COUNT(*) FROM PosStockMovements"));
            });
            Check("Ödenen stok ve hareketler yeniden açılışta korunur", () => {
                using var f=new Fixture(); f.Enable(3); f.Order.AddProductToBill(f.Bill,f.Product);
                True(f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Card).HasValue);
                new DatabaseInitializer(f.Db).Initialize();
                Equal(2,f.Item.Quantity); Equal(1,f.Stock.GetHistory(f.Product).Count(x=>x.Kind=="Satış"));
            });
            Check("Aynı sipariş satırına eşzamanlı eklemelerde adet kaybolmaz", () => {
                using var f=new Fixture(); f.Enable(4); f.Order.AddProductToBill(f.Bill,f.Product);
                Parallel.For(0,2,_=>f.Order.AddProductToBill(f.Bill,f.Product));
                Equal(3L,f.Item.Reserved); Equal(3,f.Items.GetBillItemsByBillId(f.Bill).Sum(x=>x.Quantity));
            });
            Check("Stok geçmişi bulunan ürün silinemez, pasif yapılabilir", () => {
                using var f=new Fixture(); f.Enable(1);
                try { f.Products.DeleteProduct(f.Product); throw new Exception("Delete should fail"); } catch(SqliteException) {}
                True(f.Products.SetProductInactive(f.Product)); True(!f.Item.IsActive);
            });
            Check("Bildirim yükseltmesi mevcut kayıtları korur, geçmiş işlemleri yeni bildirim diye üretmez", () => {
                using var f=new Fixture(preserveDefaults:true); f.Adjust("Sayım",42);
                f.Exec("DROP TRIGGER PosNotifyBillOpened; DROP TRIGGER PosNotifyBillClosed; DROP TRIGGER PosNotifyBillReset; DROP TRIGGER PosNotifyPayment; DROP TABLE PosTableNotifications;");
                var before=f.Snapshot("Products","Categories","Users","RestaurantTables","Bills","BillItems","Payments","PosStockMovements");
                new DatabaseInitializer(f.Db).Initialize();
                Equal(before,f.Snapshot("Products","Categories","Users","RestaurantTables","Bills","BillItems","Payments","PosStockMovements"));
                Equal(42,f.Item.Quantity); Equal(0,f.Feed.TableEvents.Count);
                new DatabaseInitializer(f.Db).Initialize();
                Equal(0,f.Feed.TableEvents.Count); Equal(1,Directory.GetFiles(Path.Combine(f.Directory,"backups"),"*.db").Length);
            });
            Check("Masa açılışı bir kez bildirilir; sıfırlama geçmişi silmez ve boş masada bildirim tekrarlamaz", () => {
                using var f=new Fixture();
                var tables=new TableManagementService(new RestaurantTableRepository(f.Db),f.Bills);
                Equal(f.Bill,tables.GetOrCreateOpenBillId(f.Table,f.Admin));
                Equal(f.Bill,tables.GetOrCreateOpenBillId(f.Table,f.Admin));
                Equal(1,f.Feed.TableEvents.Count); Equal("Opened",f.Feed.TableEvents[0].Kind);
                True(f.Bills.ForceResetTable(f.Table)); Equal(2,f.Feed.TableEvents.Count);
                Equal("Closed",f.Feed.TableEvents[0].Kind);
                True(f.Bills.ForceResetTable(f.Table)); Equal(2,f.Feed.TableEvents.Count);
                new DatabaseInitializer(f.Db).Initialize(); Equal(2,f.Feed.TableEvents.Count);
                var bill=tables.GetOrCreateOpenBillId(f.Table,f.Admin);
                True(bill!=f.Bill); Equal(3,f.Feed.TableEvents.Count);
            });
            Check("Ödeme ve kapanış birer kez bildirilir; tutar, yöntem ve masa adı korunur", () => {
                using var f=new Fixture(); f.Enable(4); f.Order.AddProductToBill(f.Bill,f.Product);
                True(f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Cash).HasValue);
                var events=f.Feed.TableEvents;
                Equal(3,events.Count); Equal("Closed",events[0].Kind); Equal("Paid",events[1].Kind);
                Equal(120m,events[1].Amount!.Value); Equal(0,events[1].PaymentType!.Value);
                True(events[1].Message.Contains("ödendi") && events[1].Message.Contains("Nakit"));
                True(!f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Card).HasValue);
                f.Exec("UPDATE RestaurantTables SET Name='Yeni Masa Adı' WHERE Id="+f.Table);
                Equal(events[1].TableName,f.Feed.TableEvents[1].TableName); Equal(3,f.Feed.TableEvents.Count);
            });
            Check("Ödeme kaydından sonra hata olursa ödeme ve kapanış bildirimleri de geri alınır", () => {
                using var f=new Fixture(); f.Enable(4); f.Order.AddProductToBill(f.Bill,f.Product);
                f.Exec("CREATE TRIGGER FailClose BEFORE UPDATE OF Status ON Bills WHEN NEW.Status=1 BEGIN SELECT RAISE(ABORT,'Test failure'); END;");
                True(!f.Payments.CreatePaymentAndCloseBillAtomic(f.Bill,f.Admin,PaymentType.Cash).HasValue);
                Equal(1,f.Feed.TableEvents.Count); Equal("Opened",f.Feed.TableEvents[0].Kind);
                Equal(4,f.Item.Quantity); Equal(0L,f.Scalar("SELECT COUNT(*) FROM Payments"));
            });
            Check("Açılış ve sıfırlama başarısız olduğunda hayalet masa bildirimi oluşmaz", () => {
                using var f=new Fixture();
                f.Exec("CREATE TRIGGER FailTable BEFORE UPDATE OF Status ON RestaurantTables BEGIN SELECT RAISE(ABORT,'Test failure'); END;");
                try { f.Bills.CreateBill(f.OtherTable,f.Admin); throw new Exception("Open should fail"); } catch(SqliteException) {}
                try { f.Bills.ForceResetTable(f.Table); throw new Exception("Reset should fail"); } catch(SqliteException) {}
                Equal(1,f.Feed.TableEvents.Count); Equal("Opened",f.Feed.TableEvents[0].Kind);
                True(f.Bills.GetOpenBillByTableId(f.Table)!=null);
            });
            Check("Stok uyarısı satılabilir adede göre kritik/tükendi olur, stok düzelince kaybolur", () => {
                using var f=new Fixture(); f.Enable(2);
                Equal(1,f.Feed.StockAlerts.Count); Equal("Kritik stok",f.Feed.StockAlerts[0].Status);
                f.Order.AddProductToBill(f.Bill,f.Product); f.Order.AddProductToBill(f.Bill,f.Product);
                Equal(2,f.Item.Quantity); Equal(0L,f.Feed.StockAlerts[0].Available); Equal("Tükendi",f.Feed.StockAlerts[0].Status);
                f.Adjust("Giriş",3); Equal(0,f.Feed.StockAlerts.Count);
                f.Adjust("Sayım",2); Equal(1,f.Feed.StockAlerts.Count);
                f.Bills.ForceResetTable(f.Table); f.Adjust("Giriş",1); Equal(0,f.Feed.StockAlerts.Count);
            });
            Check("Pasif kategori, pasif ürün ve takibi kapalı ürün stok uyarısı vermez", () => {
                using var f=new Fixture(); f.Enable(1); Equal(1,f.Feed.StockAlerts.Count);
                f.Products.SetProductInactive(f.Product); Equal(0,f.Feed.StockAlerts.Count);
                f.Exec("UPDATE Products SET IsActive=1 WHERE Id="+f.Product+"; UPDATE Categories SET IsActive=0 WHERE Id=(SELECT CategoryId FROM Products WHERE Id="+f.Product+");");
                Equal(0,f.Feed.StockAlerts.Count);
                f.Exec("UPDATE Categories SET IsActive=1;");
                f.Stock.Configure(f.Product,false,0,f.Item.Revision,f.Admin); Equal(0,f.Feed.StockAlerts.Count);
            });
            Check("Bildirim geçmişi en yeni 100 hareketi sıralı getirir, yenileme kayıt çoğaltmaz", () => {
                using var f=new Fixture();
                for(var i=0;i<55;i++) { f.Bills.ForceResetTable(f.Table); f.Bills.CreateBill(f.Table,f.Admin); }
                var events=f.Feed.TableEvents;
                Equal(100,events.Count); Equal(111L,events[0].Id); Equal(12L,events[^1].Id);
                Equal(100,events.Select(x=>x.Id).Distinct().Count());
                Equal(111L,f.Scalar("SELECT COUNT(*) FROM PosTableNotifications"));
                new DatabaseInitializer(f.Db).Initialize(); Equal(111L,f.Feed.TableEvents[0].Id);
            });
            RunRevenueChecks();
            RunStockAccessChecks();
            Console.WriteLine($"PASS: {Passed} integration checks. Databases: {Root}");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Check(string name,Action test) { test(); Passed++; Console.WriteLine("PASS "+name); }
    private static void True(bool value) { if(!value) throw new Exception("Assertion failed"); }
    private static void Equal<T>(T expected,T actual) { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Throws(Action action) { try {action();} catch(InvalidOperationException) {return;} throw new Exception("Expected InvalidOperationException"); }

    private static void VerifyCatalog(Fixture f)
    {
        using var resource=typeof(DatabaseInitializer).Assembly.GetManifestResourceStream("Rivada.Menu.2026-09-26.json")!;
        using var catalog=System.Text.Json.JsonDocument.Parse(resource);
        using var c=f.Db.CreateConnection(); c.Open();
        foreach(var item in catalog.RootElement.GetProperty("Items").EnumerateArray())
        {
            using var cmd=c.CreateCommand();
            cmd.CommandText="SELECT p.Price,p.IsActive,c.IsActive FROM Products p JOIN Categories c ON c.Id=p.CategoryId WHERE p.Name=@name AND c.Name=@category";
            cmd.Parameters.AddWithValue("@name",item.GetProperty("Name").GetString());
            cmd.Parameters.AddWithValue("@category",item.GetProperty("Category").GetString());
            using var r=cmd.ExecuteReader(); True(r.Read());
            Equal(item.GetProperty("Price").GetDecimal(),r.GetDecimal(0));
            True(r.GetBoolean(1) && r.GetBoolean(2)); True(!r.Read());
        }
    }

    private sealed class Fixture : IDisposable
    {
        public string Directory {get;}=Path.Combine(Root,Guid.NewGuid().ToString("N"));
        public SqliteDatabaseHelper Db {get;}
        public StockRepository Stock {get;}
        public BillRepository Bills {get;}
        public BillItemRepository Items {get;}
        public ProductRepository Products {get;}
        public PaymentRepository Payments {get;}
        public OrderService Order {get;}
        public int Admin,Waiter,Product,Other,Table,OtherTable,Bill;
        public StockItem Item=>Stock.GetStock().Single(x=>x.ProductId==Product);
        public NotificationFeed Feed=>new NotificationRepository(Db).GetFeed();
        public Fixture(bool preserveDefaults=false)
        {
            Db=new SqliteDatabaseHelper(Path.Combine(Directory,"restaurant.db"));
            new DatabaseInitializer(Db).Initialize();
            // Controlled zero baseline for isolated inventory tests; retain the applied migration marker.
            if(!preserveDefaults) Exec("DELETE FROM PosStockMovements; DELETE FROM PosStockBalances;");
            Stock=new StockRepository(Db); Bills=new BillRepository(Db); Items=new BillItemRepository(Db); Products=new ProductRepository(Db); Payments=new PaymentRepository(Db);
            Order=new OrderService(new CategoryRepository(Db),Products,Items,Bills,Stock);
            Admin=(int)Scalar("SELECT Id FROM Users WHERE Username='admin'"); Waiter=(int)Scalar("SELECT Id FROM Users WHERE Username='waiter1'");
            Product=(int)Scalar("SELECT Id FROM Products WHERE Name='Cola'"); Other=(int)Scalar("SELECT Id FROM Products WHERE Name='Fanta'");
            Table=(int)Scalar("SELECT Id FROM RestaurantTables LIMIT 1"); OtherTable=(int)Scalar("SELECT Id FROM RestaurantTables WHERE Id<>"+Table+" LIMIT 1");
            Bill=Bills.CreateBill(Table,Admin);
        }
        public void Enable(int quantity) { Stock.Configure(Product,true,2,0,Admin); Adjust("Giriş",quantity); }
        public void Adjust(string kind,int quantity) => Stock.Adjust(Product,kind,quantity,"Test işlemi",Item.Revision,Admin);
        public void Exec(string sql) { using var c=Db.CreateConnection(); c.Open(); using var cmd=c.CreateCommand(); cmd.CommandText=sql; cmd.ExecuteNonQuery(); }
        public long Scalar(string sql) { using var c=Db.CreateConnection(); c.Open(); using var cmd=c.CreateCommand(); cmd.CommandText=sql; return Convert.ToInt64(cmd.ExecuteScalar()); }
        public string Snapshot(params string[] tables)
        {
            var rows=new List<string>(); using var c=Db.CreateConnection(); c.Open();
            foreach(var table in tables.Length>0 ? tables : new[]{"Products","Categories","Users","RestaurantTables","Bills","BillItems","Payments"})
            {
                using var cmd=c.CreateCommand(); cmd.CommandText=$"SELECT * FROM {table} ORDER BY Id"; using var r=cmd.ExecuteReader();
                while(r.Read()) { var values=new object[r.FieldCount]; r.GetValues(values); rows.Add(table+":"+string.Join("|",values)); }
            }
            return string.Join("\n",rows);
        }
        public void Dispose() { SqliteConnection.ClearAllPools(); }
    }
}
