using System.Globalization;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.WinForms.Forms;

public sealed class RevenueForm : Form
{
    private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
    private readonly User _user;
    private readonly IRevenueService _service;
    private readonly DateTimePicker _start=DatePicker();
    private readonly DateTimePicker _end=DatePicker();
    private readonly FlowLayoutPanel _filters=new() { Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Padding=new Padding(0,8,0,12) };
    private readonly Label _total=ValueLabel();
    private readonly Label _cash=ValueLabel();
    private readonly Label _card=ValueLabel();
    private readonly Label _summary=new() { AutoSize=true,Margin=new Padding(0,8,0,5),ForeColor=Color.FromArgb(51,65,85) };
    private readonly Label _open=new() { AutoSize=true,Margin=new Padding(0,0,0,12),ForeColor=Color.FromArgb(100,116,139) };
    private readonly Label _status=new() { AutoSize=true,Margin=new Padding(0,10,0,0),Text="Rapor hazırlanıyor…" };
    private readonly DataGridView _payments=Grid();
    private readonly DataGridView _days=Grid();
    private readonly DataGridView _products=Grid();
    private readonly TabPage _paymentTab=new("Ödemeler");
    private readonly TabPage _dayTab=new("Günlük ciro");
    private readonly TabPage _productTab=new("Ürün satışları");
    private bool _loading;

    public RevenueForm(User user,IRevenueService service)
    {
        if(!string.Equals(user.RoleName,"Admin",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ciro raporunu yalnızca yönetici görüntüleyebilir.");
        _user=user; _service=service;
        Text="Rivada Restaurant — Ciro Takibi";
        Font=new Font("Segoe UI",9.5F); AutoScaleMode=AutoScaleMode.Font;
        StartPosition=FormStartPosition.CenterParent;
        Size=new Size(1220,800); MinimumSize=new Size(1050,700);
        BackColor=Color.FromArgb(239,244,249);
        var root=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,Padding=new Padding(22,16,22,16) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,108));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label { Text="Ciro Takibi",AutoSize=true,Font=new Font("Segoe UI",21,FontStyle.Bold),ForeColor=Color.FromArgb(15,23,42) },0,0);

        foreach(var name in new[] {"Bugün","Dün","Bu hafta","Bu ay"})
        {
            var preset=Button(name);
            preset.Click+=async (_,_)=> { SelectPeriod(name); await LoadReportAsync(); };
            _filters.Controls.Add(preset);
        }
        _filters.Controls.Add(FilterLabel("Başlangıç")); _filters.Controls.Add(_start);
        _filters.Controls.Add(FilterLabel("Bitiş")); _filters.Controls.Add(_end);
        var apply=Button("Raporu göster");
        apply.BackColor=Color.FromArgb(37,99,235); apply.ForeColor=Color.White;
        apply.Click+=async (_,_)=>await LoadReportAsync();
        _filters.Controls.Add(apply); AcceptButton=apply;
        root.Controls.Add(_filters,0,1);

        var cards=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty };
        for(var i=0;i<3;i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100F/3));
        cards.Controls.Add(Card("TAHSİL EDİLEN CİRO",_total,Color.FromArgb(29,78,216)),0,0);
        cards.Controls.Add(Card("NAKİT",_cash,Color.FromArgb(21,128,61)),1,0);
        cards.Controls.Add(Card("KART",_card,Color.FromArgb(109,40,217)),2,0);
        root.Controls.Add(cards,0,2); root.Controls.Add(_summary,0,3); root.Controls.Add(_open,0,4);

        Column(_payments,"PaidAt","Ödeme zamanı",152,"dd.MM.yyyy HH:mm:ss");
        Column(_payments,"BillId","Adisyon",75);
        Column(_payments,"TableName","Masa",100);
        Column(_payments,"Method","Ödeme",80);
        Column(_payments,"Amount","Tutar",135,"C2");
        Column(_payments,"Username","İşlemi yapan",125);
        Column(_days,"Date","Gün",125,"dd.MM.yyyy");
        Column(_days,"PaymentCount","Ödenen adisyon",120);
        Column(_days,"Cash","Nakit",140,"C2");
        Column(_days,"Card","Kart",140,"C2");
        Column(_days,"Total","Ciro",150,"C2");
        Column(_products,"ProductName","Satıştaki ürün adı",280);
        Column(_products,"Quantity","Satılan adet",120);
        Column(_products,"Amount","Satış tutarı",170,"C2");
        AddTabContent(_paymentTab,_payments,"Ödeme tarihine göre sıralanır. Açık ve sıfırlanmış adisyonlar ciroya eklenmez.");
        AddTabContent(_dayTab,_days,"Ödeme bulunan günler listelenir. Gün sınırı yerel saatle 00.00'dır.");
        AddTabContent(_productTab,_products,"Yalnızca ödenen adisyonlar; satış anındaki ürün adı ve tutarı kullanılır.");
        var tabs=new TabControl { Dock=DockStyle.Fill };
        tabs.TabPages.AddRange(new[] {_paymentTab,_dayTab,_productTab});
        root.Controls.Add(tabs,0,5); root.Controls.Add(_status,0,6);
        Controls.Add(root);
        SelectPeriod("Bugün");
        Shown+=async (_,_)=>await LoadReportAsync();
    }

    private void SelectPeriod(string period)
    {
        var today=DateTime.Now.AddHours(-_service.BusinessDayStartHour).Date;
        _end.Value=today;
        _start.Value=period switch {
            "Dün"=>today.AddDays(-1),
            "Bu hafta"=>today.AddDays(-((7+(int)today.DayOfWeek-(int)DayOfWeek.Monday)%7)),
            "Bu ay"=>new DateTime(today.Year,today.Month,1),
            _=>today
        };
        if(period=="Dün") _end.Value=_start.Value;
    }

    private async Task LoadReportAsync()
    {
        if(_loading || IsDisposed || Disposing) return;
        var start=_start.Value.Date; var end=_end.Value.Date;
        _loading=true; _filters.Enabled=false; UseWaitCursor=true;
        _status.ForeColor=Color.FromArgb(100,116,139); _status.Text="Rapor hazırlanıyor…";
        try
        {
            var report=await Task.Run(()=>_service.GetReport(start,end,_user.Id));
            if(IsDisposed || Disposing) return;
            _total.Text=report.Total.ToString("C2",Turkish);
            _cash.Text=report.Cash.ToString("C2",Turkish);
            _card.Text=report.Card.ToString("C2",Turkish);
            _summary.Text=$"{start:dd.MM.yyyy} – {end:dd.MM.yyyy}   ·   {report.PaymentCount:N0} ödenen adisyon   ·   Ortalama hesap: {report.Average.ToString("C2",Turkish)}";
            _open.Text=$"Şu an açık: {report.OpenBillCount} adisyon · {report.OpenBillAmount.ToString("C2",Turkish)}   —   Tarih filtresinden bağımsızdır, ciroya dahil değildir.";
            _payments.DataSource=report.Payments; _days.DataSource=report.Days; _products.DataSource=report.Products;
            _paymentTab.Text=$"Ödemeler ({report.PaymentCount:N0})";
            _dayTab.Text=$"Günlük ciro ({report.Days.Count:N0} gün)";
            _productTab.Text=$"Ürün satışları ({report.Products.Count:N0})";
            _status.Text=report.ProductTotalMismatch ? "Ürün toplamı ile ödeme tutarı farklı. Ciro, kaydedilen ödeme tutarlarını esas alır."
                : report.PaymentCount==0 ? $"Bu tarih aralığında ödeme kaydı yok. Son güncelleme: {report.RefreshedAt:HH:mm:ss}"
                : $"Son güncelleme: {report.RefreshedAt:dd.MM.yyyy HH:mm:ss} · Tüm tutarlar Türk lirası";
            _status.ForeColor=report.ProductTotalMismatch ? Color.FromArgb(146,64,14) : Color.FromArgb(100,116,139);
        }
        catch(Exception ex)
        {
            if(!IsDisposed && !Disposing)
            {
                _total.Text=_cash.Text=_card.Text="—";
                _summary.Text=_open.Text="";
                _payments.DataSource=_days.DataSource=_products.DataSource=null;
                _paymentTab.Text="Ödemeler"; _dayTab.Text="Günlük ciro"; _productTab.Text="Ürün satışları";
                _status.ForeColor=Color.FromArgb(153,27,27);
                _status.Text=ex is InvalidOperationException ? ex.Message : "Ciro raporu yüklenemedi. Raporu göster düğmesiyle tekrar deneyin.";
            }
        }
        finally
        {
            _loading=false;
            if(!IsDisposed && !Disposing) { _filters.Enabled=true; UseWaitCursor=false; }
        }
    }

    private static DateTimePicker DatePicker() => new() { Format=DateTimePickerFormat.Custom,CustomFormat="dd.MM.yyyy",Width=120,Margin=new Padding(3,6,9,3) };
    private static Label FilterLabel(string text) => new() { Text=text,AutoSize=true,Margin=new Padding(8,10,3,0) };
    private static Label ValueLabel() => new() { Dock=DockStyle.Fill,Text="—",TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,Font=new Font("Segoe UI",24,FontStyle.Bold) };
    private static Button Button(string text)
    {
        var button=new Button { Text=text,AutoSize=true,Height=34,MinimumSize=new Size(66,34),FlatStyle=FlatStyle.Flat,
            BackColor=Color.White,ForeColor=Color.FromArgb(51,65,85),Cursor=Cursors.Hand,Margin=new Padding(0,3,6,3) };
        button.FlatAppearance.BorderSize=0;
        return button;
    }
    private static Control Card(string title,Label value,Color color)
    {
        var panel=new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(16,12,16,8),BackColor=Color.White,Margin=new Padding(0,0,12,0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute,22)); panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        panel.Controls.Add(new Label { Text=title,AutoSize=true,ForeColor=Color.FromArgb(100,116,139),Font=new Font("Segoe UI",9,FontStyle.Bold) },0,0);
        value.ForeColor=color; panel.Controls.Add(value,0,1);
        return panel;
    }
    private static void AddTabContent(TabPage tab,Control grid,string text)
    {
        tab.Padding=new Padding(8); tab.BackColor=Color.White;
        tab.Controls.Add(grid);
        tab.Controls.Add(new Label { Text=text,Dock=DockStyle.Top,Height=34,ForeColor=Color.FromArgb(100,116,139),Padding=new Padding(0,6,0,0) });
    }
    private static DataGridView Grid() => new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,
        AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,MultiSelect=false,
        SelectionMode=DataGridViewSelectionMode.FullRowSelect,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,
        AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowTemplate={Height=34},ColumnHeadersHeight=36 };
    private static void Column(DataGridView grid,string property,string title,int width,string? format=null) =>
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName=property,HeaderText=title,MinimumWidth=width,FillWeight=width,
            SortMode=DataGridViewColumnSortMode.NotSortable,DefaultCellStyle=new DataGridViewCellStyle { Format=format??"",FormatProvider=Turkish,
                Alignment=format=="C2" ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft } });
}
