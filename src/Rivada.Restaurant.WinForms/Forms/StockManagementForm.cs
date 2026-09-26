using System.Drawing;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.WinForms.Infrastructure;

namespace Rivada.Restaurant.WinForms.Forms;

public sealed class StockManagementForm : Form
{
    private readonly User _user;
    private readonly IStockService _service;
    private readonly DataGridView _grid = CreateGrid();
    private readonly DataGridView _history = CreateGrid();
    private readonly TextBox _search = new() { Width = 230, PlaceholderText = "Ürün veya kategori ara" };
    private readonly CheckBox _low = new() { Text = "Kritik / tükenen", AutoSize = true };
    private readonly CheckBox _inactive = new() { Text = "Pasif ürünler dahil", AutoSize = true };
    private readonly Label _summary = new() { AutoSize = true, Padding = new Padding(6), ForeColor = Color.FromArgb(51,65,85) };
    private readonly Label _selected = new() { AutoSize = true, MaximumSize = new Size(265,0), Font = new Font("Segoe UI", 12, FontStyle.Bold) };
    private readonly Label _balance = new() { AutoSize = true, MaximumSize = new Size(265,0), Margin = new Padding(3,10,3,12) };
    private readonly CheckBox _tracked = new() { Text = "Bu ürünün stok takibini aç", AutoSize = true };
    private readonly NumericUpDown _minimum = Number();
    private readonly ComboBox _kind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    private readonly NumericUpDown _quantity = Number();
    private readonly Label _quantityHelp = new() { AutoSize = true, MaximumSize = new Size(255,0), ForeColor = Color.FromArgb(71,85,105) };
    private List<StockItem> _rows = new();
    private bool _loading;
    private bool _busy;
    private StockItem? Selected => _grid.CurrentRow?.DataBoundItem as StockItem;

    public static void ShowForUser(User user, IStockService service, IWin32Window? owner = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(service);
        if (!string.Equals(user.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bu ekran için yönetici yetkisi gereklidir.");

        // Every opening gets a new prompt. Stock data is loaded only after acceptance.
        using var password = new StockPasswordForm();
        if (password.ShowDialog(owner) != DialogResult.OK) return;
        using var form = new StockManagementForm(user, service);
        form.ShowDialog(owner);
    }

    private StockManagementForm(User user, IStockService service)
    {
        if (!string.Equals(user.RoleName,"Admin",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bu ekran için yönetici yetkisi gereklidir.");
        _user = user; _service = service;
        Text = "Rivada Restaurant — Stok Takibi";
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1220,800);
        MinimumSize = new Size(1024,720);
        BackColor = Color.FromArgb(239,244,249);

        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=3, Padding=new Padding(14) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,292));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var top = new FlowLayoutPanel { Dock=DockStyle.Fill, AutoSize=true, Padding=new Padding(0,0,0,8), WrapContents=true };
        var refresh = Button("Yenile", async () => await Reload());
        top.Controls.AddRange(new Control[] {_search,_low,_inactive,refresh});
        root.Controls.Add(top,0,0); root.SetColumnSpan(top,2);
        root.Controls.Add(_summary,0,1); root.SetColumnSpan(_summary,2);

        var lists = new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=3, ColumnCount=1 };
        lists.RowStyles.Add(new RowStyle(SizeType.Percent,60));
        lists.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        lists.RowStyles.Add(new RowStyle(SizeType.Percent,40));
        lists.Controls.Add(_grid,0,0);
        lists.Controls.Add(new Label { Text="Seçili ürünün hareketleri · son 1.000 kayıt · yerel saat", AutoSize=true, Padding=new Padding(2,8,0,6) },0,1);
        lists.Controls.Add(_history,0,2);
        root.Controls.Add(lists,0,2);

        var editor = new FlowLayoutPanel { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false,
            AutoScroll=true, BackColor=Color.White, Padding=new Padding(12), Margin=new Padding(10,0,0,0) };
        editor.Controls.Add(_selected);
        editor.Controls.Add(_balance);
        editor.Controls.Add(_tracked);
        editor.Controls.Add(Label("Kritik stok eşiği (adet)"));
        editor.Controls.Add(_minimum);
        editor.Controls.Add(Button("Takip ayarını kaydet", async () => {
            var item = RequireSelected();
            var tracked = _tracked.Checked; var minimum = (int)_minimum.Value;
            await Task.Run(() => _service.Configure(item.ProductId,tracked,minimum,item.Revision,_user.Id));
            await Reload();
        }));
        editor.Controls.Add(Label("Stok hareketi", true));
        _kind.Items.AddRange(new object[] {"Giriş","Fire / Çıkış","Sayım"});
        _kind.SelectedIndex=0;
        editor.Controls.Add(_kind);
        editor.Controls.Add(Label("Adet"));
        editor.Controls.Add(_quantity);
        editor.Controls.Add(_quantityHelp);
        editor.Controls.Add(Button("Hareketi kaydet", async () => {
            var item = RequireSelected();
            var kind = _kind.Text; var quantity = (int)_quantity.Value;
            await Task.Run(() => _service.Adjust(item.ProductId,kind,quantity,"",item.Revision,_user.Id));
            _quantity.Value=0;
            await Reload();
        }));
        editor.Controls.Add(new Label { AutoSize=true, MaximumSize=new Size(255,0), Margin=new Padding(3,15,3,0),
            ForeColor=Color.FromArgb(71,85,105), Text="Açık adisyon adedi satışa ayrılır. Ödeme tamamlanınca kayıtlı stok azalır; iptal edilince ayrılan adet serbest kalır.\n\nGiriş ile stok ekleyin, Fire / Çıkış ile azaltın, Sayım ile toplam adedi düzenleyin." });
        root.Controls.Add(editor,1,2);
        Controls.Add(root);

        Column(_grid,"ProductName","Ürün",160);
        Column(_grid,"CategoryName","Kategori",115);
        Column(_grid,"Quantity","Kayıtlı",65);
        Column(_grid,"Reserved","Ayrılan",65);
        Column(_grid,"Available","Satılabilir",75);
        Column(_grid,"MinimumQuantity","Eşik",55);
        Column(_grid,"Status","Durum",90);
        Column(_history,"Date","Tarih",130,"dd.MM.yyyy HH:mm");
        Column(_history,"Kind","İşlem",90);
        Column(_history,"QuantityChange","Değişim",65,"+0;-0;0");
        Column(_history,"BalanceAfter","Son stok",65);
        Column(_history,"Username","Kullanıcı",85);
        Column(_history,"BillId","Adisyon",65);
        _grid.SelectionChanged += (_,_) => { if (!_loading && !_busy) Run(ShowSelection); };
        _grid.RowPrePaint += (_,e) => {
            if (_grid.Rows[e.RowIndex].DataBoundItem is StockItem item)
                _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = item.IsTracked && item.Available <= item.MinimumQuantity
                    ? Color.FromArgb(254,226,226) : Color.White;
        };
        _search.TextChanged += (_,_) => { if (!_busy) Run(Filter); };
        _low.CheckedChanged += (_,_) => { if (!_busy) Run(Filter); };
        _inactive.CheckedChanged += (_,_) => { if (!_busy) Run(Filter); };
        _kind.SelectedIndexChanged += (_,_) => UpdateQuantityHelp();
        UpdateQuantityHelp();
        Shown += (_,_) => Run(Reload);
    }

    private StockItem RequireSelected() => Selected ?? throw new InvalidOperationException("Listeden bir ürün seçin.");

    private async Task Reload()
    {
        _rows = await Task.Run(_service.GetStock);
        _summary.Text = $"Takip edilen: {_rows.Count(x=>x.IsTracked)} ürün     Kritik / tükenen: {_rows.Count(x=>x.IsTracked && x.Available<=x.MinimumQuantity)} ürün     Stok birimi: adet";
        await Filter();
    }

    private async Task Filter()
    {
        var selectedId = Selected?.ProductId;
        var search = _search.Text.Trim();
        var rows = _rows.Where(x => (_inactive.Checked || x.IsActive)
            && (!_low.Checked || x.IsTracked && x.Available <= x.MinimumQuantity)
            && (search.Length==0 || x.ProductName.Contains(search,StringComparison.CurrentCultureIgnoreCase)
                || x.CategoryName.Contains(search,StringComparison.CurrentCultureIgnoreCase))).ToList();
        _loading=true;
        try {
            _grid.DataSource=rows;
            if (selectedId.HasValue)
                foreach (DataGridViewRow row in _grid.Rows)
                    if (row.DataBoundItem is StockItem item && item.ProductId==selectedId.Value) { _grid.CurrentCell=row.Cells[0]; break; }
        } finally { _loading=false; }
        await ShowSelection();
    }

    private async Task ShowSelection()
    {
        var item=Selected;
        _selected.Text=item?.ProductName ?? "Ürün seçin";
        _balance.Text=item==null ? "" : $"Kayıtlı: {item.Quantity} adet\nAçık adisyon: {item.Reserved} adet\nSatılabilir: {item.Available} adet";
        _tracked.Checked=item?.IsTracked ?? false;
        _minimum.Value=item?.MinimumQuantity ?? 0;
        _quantity.Value=0;
        _history.DataSource=item==null ? new List<StockMovement>() : await Task.Run(()=>_service.GetHistory(item.ProductId));
    }

    private void UpdateQuantityHelp() => _quantityHelp.Text = _kind.Text=="Sayım"
        ? "Yeni toplam kayıtlı adedi girin; açık adisyonlara ayrılan adetleri de dahil edin."
        : _kind.Text=="Giriş" ? "Stoğa eklenecek adedi girin." : "Stoktan çıkarılacak adedi girin. Satış için ödeme ekranını kullanın.";

    private async void Run(Func<Task> action)
    {
        if (_busy) return;
        _busy=true; UseWaitCursor=true; Enabled=false;
        try { await action(); }
        catch (InvalidOperationException ex) { MessageBox.Show(this,ex.Message,"Stok işlemi",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        catch (Exception ex) { AppDiagnostics.ShowError(this,"Stok işlemi tamamlanamadı.",ex,"StockManagementForm"); }
        finally { Enabled=true; UseWaitCursor=false; _busy=false; }
    }

    private Button Button(string text, Func<Task> action)
    {
        var button = new Button { Text=text, AutoSize=true, MinimumSize=new Size(100,34), FlatStyle=FlatStyle.Flat,
            BackColor=Color.FromArgb(226,232,240), Margin=new Padding(3,6,3,6) };
        button.FlatAppearance.BorderSize=0;
        button.Click += (_,_) => Run(action);
        return button;
    }
    private static Label Label(string text, bool heading=false) => new() { Text=text, AutoSize=true,
        Margin=new Padding(3,heading?18:8,3,4), Font=new Font("Segoe UI",9,heading?FontStyle.Bold:FontStyle.Regular) };
    private static NumericUpDown Number() => new() { Maximum=2000000000, Width=250, ThousandsSeparator=true };
    private static DataGridView CreateGrid() => new() { Dock=DockStyle.Fill, ReadOnly=true, AllowUserToAddRows=false,
        AllowUserToDeleteRows=false, AutoGenerateColumns=false, RowHeadersVisible=false, MultiSelect=false,
        SelectionMode=DataGridViewSelectionMode.FullRowSelect, BackgroundColor=Color.White, BorderStyle=BorderStyle.None,
        AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill, RowTemplate={Height=32}, ColumnHeadersHeight=38 };
    private static void Column(DataGridView grid,string property,string title,int width,string? format=null)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName=property, HeaderText=title,
            MinimumWidth=width, FillWeight=width, SortMode=DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle=new DataGridViewCellStyle { Format=format??"" } });
    }
}
