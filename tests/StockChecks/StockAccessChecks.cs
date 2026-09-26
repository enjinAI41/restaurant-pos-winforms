using System.Diagnostics;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.WinForms.Forms;

internal static partial class Program
{
    private static void RunStockAccessChecks()
    {
        Check("Stok erişimi: boş, yanlış ve fazla karakterli şifreler ekranı açmaz; iptal veri okumaz", () =>
        {
            var service = new StockAccessProbe();
            RunStockPrompt(service, prompt =>
            {
                var password = (TextBox)prompt.Controls.Find("StockPassword", true).Single();
                True(password.UseSystemPasswordChar);
                foreach (var invalid in new[] { "", "0000", "41280", " 4128", "4128 " })
                {
                    password.Text = invalid;
                    ((Button)prompt.Controls.Find("UnlockStock", true).Single()).PerformClick();
                    Equal(DialogResult.None, prompt.DialogResult);
                    Equal("", password.Text);
                    True(prompt.Controls.Find("PasswordError", true).Single().Text.Length > 0);
                    Equal(0, service.Reads);
                    True(!Application.OpenForms.Cast<Form>().Any(x => x is StockManagementForm));
                }
                ((Button)prompt.Controls.Find("CancelStock", true).Single()).PerformClick();
            }, expectStock: false);
            Equal(0, service.Reads);
        });
        Check("Stok erişimi: doğru şifre açar, kapatıp yeniden girince boş şifre tekrar sorulur", () =>
        {
            var service = new StockAccessProbe();
            RunStockPrompt(service, prompt =>
            {
                ((TextBox)prompt.Controls.Find("StockPassword", true).Single()).Text = "4128";
                ((Button)prompt.Controls.Find("UnlockStock", true).Single()).PerformClick();
            }, expectStock: true);
            Equal(1, service.Reads);
            RunStockPrompt(service, prompt =>
            {
                Equal("", ((TextBox)prompt.Controls.Find("StockPassword", true).Single()).Text);
                prompt.Close(); // Window close has the same denial semantics as Cancel/Escape.
            }, expectStock: false);
            Equal(1, service.Reads);
        });
        Check("Stok erişimi: yönetici olmayan kullanıcı şifre ekranına bile giremez", () =>
        {
            var service = new StockAccessProbe();
            Throws(() => StockManagementForm.ShowForUser(new User { RoleName = "Waiter" }, service));
            Equal(0, service.Reads);
        });
    }

    // Exercise the actual modal entry point in a separate process with a fake, empty service.
    // No OS input, stored credentials or live business data are used.
    private static void RunStockPrompt(StockAccessProbe service, Action<Form> respond, bool expectStock)
    {
        using var timer = new System.Windows.Forms.Timer { Interval = 40 };
        var clock = Stopwatch.StartNew();
        Exception? failure = null;
        var prompted = false;
        var stockOpened = false;
        timer.Tick += (_, _) =>
        {
            try
            {
                if (clock.Elapsed > TimeSpan.FromSeconds(10)) throw new Exception("Stok erişim kontrolü zaman aşımına uğradı.");
                var prompt = Application.OpenForms.Cast<Form>().SingleOrDefault(x => x.Name == "StockPasswordForm");
                if (prompt is not null && !prompted)
                {
                    prompted = true;
                    respond(prompt);
                }
                var stock = Application.OpenForms.Cast<Form>().OfType<StockManagementForm>().SingleOrDefault();
                if (stock is not null && service.Reads > 0)
                {
                    stockOpened = true;
                    stock.Close();
                }
            }
            catch (Exception ex)
            {
                failure = ex;
                timer.Stop();
                foreach (var form in Application.OpenForms.Cast<Form>().ToArray()) form.Close();
            }
        };
        timer.Start();
        StockManagementForm.ShowForUser(new User { Id = 1, RoleName = "Admin" }, service);
        timer.Stop();
        if (failure is not null) throw new Exception("Stok şifre akışı başarısız.", failure);
        True(prompted);
        Equal(expectStock, stockOpened);
    }

    private sealed class StockAccessProbe : IStockService
    {
        private int _reads;
        public int Reads => Volatile.Read(ref _reads);
        public List<StockItem> GetStock() { Interlocked.Increment(ref _reads); return new(); }
        public List<StockMovement> GetHistory(int productId) => throw new Exception("Unexpected history read");
        public void Configure(int productId, bool tracked, int minimum, long revision, int userId) => throw new Exception("Unexpected write");
        public void Adjust(int productId, string kind, int quantity, string note, long revision, int userId) => throw new Exception("Unexpected write");
    }
}
