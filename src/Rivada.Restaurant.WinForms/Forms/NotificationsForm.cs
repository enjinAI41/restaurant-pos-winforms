using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.WinForms.Forms;

internal sealed class NotificationsForm : Form
{
    private readonly INotificationService _service;
    private readonly DataGridView _stock = Grid();
    private readonly DataGridView _events = Grid();
    private readonly Label _status = new() { Dock=DockStyle.Top, Height=42, Padding=new Padding(8), Text="Bildirimler yükleniyor…" };
    private readonly TabPage _stockTab = new("Stok uyarıları");
    private readonly TabPage _eventTab = new("Masa hareketleri");
    private readonly System.Windows.Forms.Timer _timer = new() { Interval=15000 };
    private bool _loading;
    private bool _loadFailed;

    public NotificationsForm(INotificationService service,int selectedTab)
    {
        _service=service;
        Text="Rivada Restaurant — Bildirimler";
        Font=new Font("Segoe UI",9F);
        AutoScaleMode=AutoScaleMode.Font;
        StartPosition=FormStartPosition.CenterParent;
        Size=new Size(820,540); MinimumSize=new Size(660,400);
        BackColor=Color.FromArgb(248,250,252); Padding=new Padding(12);
        Column(_stock,"ProductName","Ürün",220);
        Column(_stock,"Status","Durum",120);
        Column(_stock,"Available","Satılabilir adet",120);
        Column(_stock,"MinimumQuantity","Kritik eşik",100);
        Column(_events,"Date","Tarih / saat",138,"dd.MM.yyyy HH:mm:ss");
        Column(_events,"Message","Bildirim",340);
        Column(_events,"BillId","Adisyon",72);
        _stock.CellFormatting+=(_,e)=> {
            if (e.RowIndex>=0 && _stock.Rows[e.RowIndex].DataBoundItem is Rivada.Restaurant.Models.StockAlert item)
                e.CellStyle!.BackColor=item.Available<=0 ? Color.FromArgb(254,226,226) : Color.FromArgb(254,243,199);
        };
        _stockTab.Controls.Add(_stock); _eventTab.Controls.Add(_events);
        var tabs=new TabControl { Dock=DockStyle.Fill };
        tabs.TabPages.AddRange(new[] {_stockTab,_eventTab}); tabs.SelectedIndex=selectedTab;
        var footer=new FlowLayoutPanel { Dock=DockStyle.Bottom, Height=44, FlowDirection=FlowDirection.RightToLeft, Padding=new Padding(0,6,0,0) };
        var close=new Button { Text="Kapat", AutoSize=true, Height=30, DialogResult=DialogResult.Cancel };
        var refresh=new Button { Text="Yenile", AutoSize=true, Height=30 };
        refresh.Click+=async (_,_)=>await RefreshAsync();
        footer.Controls.Add(close); footer.Controls.Add(refresh); CancelButton=close;
        Controls.Add(tabs); Controls.Add(_status); Controls.Add(footer);
        Shown+=async (_,_)=> { await RefreshAsync(); if (!IsDisposed) _timer.Start(); };
        _timer.Tick+=async (_,_)=>await RefreshAsync();
        tabs.SelectedIndexChanged+=(_,_)=>UpdateStatus(tabs.SelectedIndex);
    }

    private async Task RefreshAsync()
    {
        if (_loading || IsDisposed || Disposing) return;
        _loading=true;
        try
        {
            var feed=await Task.Run(_service.GetFeed);
            if (IsDisposed || Disposing) return;
            _loadFailed=false;
            _stock.DataSource=feed.StockAlerts;
            _events.DataSource=feed.TableEvents;
            _stockTab.Text=$"Stok uyarıları ({feed.StockAlerts.Count})";
            _eventTab.Text=$"Masa hareketleri ({feed.TableEvents.Count})";
            UpdateStatus(((TabControl)_stockTab.Parent!).SelectedIndex);
        }
        catch
        {
            if (!IsDisposed && !Disposing) { _loadFailed=true; UpdateStatus(0); }
        }
        finally { _loading=false; }
    }

    private void UpdateStatus(int tab) => _status.Text=_loadFailed ? "Bildirimler yenilenemedi. Görünen kayıtlar güncel olmayabilir; tekrar deneyin." : tab==0
        ? _stock.Rows.Count==0 ? "Stok uyarısı yok." : "Açık adisyonlara ayrılan miktar düşüldükten sonraki satılabilir adetler gösterilir."
        : _events.Rows.Count==0 ? "Henüz masa bildirimi yok. Yeni işlemler burada görünecek." : "Son 100 masa hareketi · en yeni üstte · yerel saat · 15 saniyede bir yenilenir";

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    private static DataGridView Grid() => new() { Dock=DockStyle.Fill, ReadOnly=true, AutoGenerateColumns=false,
        AllowUserToAddRows=false, AllowUserToDeleteRows=false, RowHeadersVisible=false, MultiSelect=false,
        SelectionMode=DataGridViewSelectionMode.FullRowSelect, BackgroundColor=Color.White, BorderStyle=BorderStyle.None,
        AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill, RowTemplate={Height=32}, ColumnHeadersHeight=36 };

    private static void Column(DataGridView grid,string property,string title,int width,string? format=null) =>
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName=property, HeaderText=title, MinimumWidth=width,
            FillWeight=width, SortMode=DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle=new DataGridViewCellStyle { Format=format??"" } });
}
