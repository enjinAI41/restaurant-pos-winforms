using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.WinForms.Forms;

internal sealed class NotificationBar : UserControl
{
    private readonly INotificationService _service;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval=15000 };
    private readonly ToolTip _tooltip = new();
    private readonly Label _stock = new() { Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, AutoEllipsis=true, Cursor=Cursors.Hand };
    private readonly Label _latest = new() { Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, AutoEllipsis=true, Cursor=Cursors.Hand };
    private bool _loading;

    public NotificationBar(INotificationService service)
    {
        _service=service;
        Dock=DockStyle.Top; Height=54;
        BackColor=Color.FromArgb(248,250,252);
        ForeColor=Color.FromArgb(51,65,85);
        Font=new Font("Segoe UI",9F);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=4, RowCount=1, Padding=new Padding(24,7,20,7) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));
        layout.Controls.Add(new Label { Text="Bildirimler", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft,
            Font=new Font("Segoe UI",9F,FontStyle.Bold) },0,0);
        _stock.Text="Stok kontrol ediliyor…";
        _stock.Padding=new Padding(8,0,4,0);
        _latest.Text="Masa hareketleri yükleniyor…";
        _latest.Padding=new Padding(12,0,6,0);
        layout.Controls.Add(_stock,1,0); layout.Controls.Add(_latest,2,0);
        var details=new Button { Text="Tümünü gör", Dock=DockStyle.Fill, FlatStyle=FlatStyle.Flat,
            BackColor=Color.FromArgb(226,232,240), Cursor=Cursors.Hand };
        details.FlatAppearance.BorderSize=0;
        details.Click+=(_,_)=>ShowDetails(0);
        _stock.Click+=(_,_)=>ShowDetails(0);
        _latest.Click+=(_,_)=>ShowDetails(1);
        layout.Controls.Add(details,3,0);
        Controls.Add(layout);
        _timer.Tick+=async (_,_)=> { if (Visible && Enabled) await RefreshAsync(); };
        HandleCreated+=(_,_)=>_timer.Start();
    }

    public async Task RefreshAsync()
    {
        if (_loading || IsDisposed || Disposing) return;
        _loading=true;
        try
        {
            var feed=await Task.Run(_service.GetFeed);
            if (IsDisposed || Disposing) return;
            var empty=feed.StockAlerts.Count(x=>x.Available<=0);
            var critical=feed.StockAlerts.Count-empty;
            _stock.Text=feed.StockAlerts.Count==0 ? "Stok uyarısı yok" : $"{empty} tükenen · {critical} kritik stok";
            _stock.BackColor=empty>0 ? Color.FromArgb(254,226,226) : critical>0 ? Color.FromArgb(254,243,199) : BackColor;
            _stock.ForeColor=empty>0 ? Color.FromArgb(153,27,27) : critical>0 ? Color.FromArgb(146,64,14) : Color.FromArgb(71,85,105);
            var latest=feed.TableEvents.FirstOrDefault();
            var text=latest?.Message ?? "Henüz masa bildirimi yok";
            if (latest?.Kind=="Closed" && feed.TableEvents.FirstOrDefault(x=>x.BillId==latest.BillId && x.Kind=="Paid") is { } paid)
                text=paid.Message+" · masa kapandı";
            _latest.Text=latest==null ? text : $"{latest.Date:HH:mm} · {text}";
            _tooltip.SetToolTip(_latest,_latest.Text);
            _tooltip.SetToolTip(_stock,feed.StockAlerts.Count==0 ? "Satılabilir adetler stok eşiklerinin üzerinde."
                : string.Join("\n",feed.StockAlerts.Take(5).Select(x=>$"{x.ProductName}: {x.Available} adet ({x.Status})")));
        }
        catch
        {
            if (!IsDisposed && !Disposing)
            {
                _stock.Text="Stok bilgisi güncellenemedi";
                _stock.BackColor=Color.FromArgb(254,243,199);
                _latest.Text="Bildirimler yenilenemedi · tekrar denenecek";
                _tooltip.SetToolTip(_stock,"Veriler güncel olmayabilir. Yenile düğmesini kullanabilirsiniz.");
                _tooltip.SetToolTip(_latest,_latest.Text);
            }
        }
        finally { _loading=false; }
    }

    private async void ShowDetails(int tab)
    {
        using var form=new NotificationsForm(_service,tab);
        form.ShowDialog(FindForm());
        await RefreshAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _timer.Dispose(); _tooltip.Dispose(); }
        base.Dispose(disposing);
    }
}
