using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;
using Rivada.Restaurant.WinForms.Infrastructure;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class TableManagementForm : Form
    {
        private readonly User _loggedInUser;
        private readonly ITableManagementService _tableManagementService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IProductManagementService _productManagementService;
        private readonly IStockService _stockService;
        private readonly NotificationBar _notificationBar;
        private readonly IRevenueService _revenueService;
        private bool _isOpeningTable;
        private bool _isResettingTable;

        public TableManagementForm(
            User loggedInUser,
            ITableManagementService tableManagementService,
            IOrderService orderService,
            IPaymentService paymentService,
            IProductManagementService productManagementService,
            IStockService stockService,
            INotificationService notificationService,
            IRevenueService revenueService)
        {
            _loggedInUser = loggedInUser ?? throw new ArgumentNullException(nameof(loggedInUser));
            _tableManagementService = tableManagementService ?? throw new ArgumentNullException(nameof(tableManagementService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _productManagementService = productManagementService ?? throw new ArgumentNullException(nameof(productManagementService));
            _stockService = stockService;
            _revenueService = revenueService ?? throw new ArgumentNullException(nameof(revenueService));

            InitializeComponent();
            _notificationBar = new NotificationBar(notificationService ?? throw new ArgumentNullException(nameof(notificationService)));
            Controls.Add(_notificationBar);
            Controls.SetChildIndex(_notificationBar, 1);
            ConfigureVisualStyle();
            SetUserHeader();
            ConfigureMenuAccess();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await LoadAndRenderTablesAsync();
        }

        private void SetUserHeader()
        {
            var fullName = string.IsNullOrWhiteSpace(_loggedInUser.FullName)
                ? _loggedInUser.Username
                : _loggedInUser.FullName;

            lblWelcome.Text = $"Kullanici: {fullName}";
            var roleName = string.IsNullOrWhiteSpace(_loggedInUser.RoleName)
                ? $"Rol ID: {_loggedInUser.RoleId}"
                : _loggedInUser.RoleName;

            lblRole.Text = $"Rol: {roleName}";
        }

        private void ConfigureMenuAccess()
        {
            btnProducts.Visible = IsAdminUser();
            btnStock.Visible = IsAdminUser();
            btnPayment.Visible = IsAdminUser();
        }

        private async void btnStock_Click(object? sender, EventArgs e)
        {
            if (!IsAdminUser()) return;
            StockManagementForm.ShowForUser(_loggedInUser, _stockService, this);
            await _notificationBar.RefreshAsync();
        }

        private bool IsAdminUser()
        {
            return string.Equals(_loggedInUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private void ConfigureVisualStyle()
        {
            flpTables.FlowDirection = FlowDirection.LeftToRight;
            flpTables.AutoScroll = true;
            // Keep all navigation buttons reachable at the minimum window width.
            var actions=new FlowLayoutPanel { Dock=DockStyle.Right,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,
                FlowDirection=FlowDirection.LeftToRight,WrapContents=false };
            foreach(var button in new[] {btnProducts,btnStock,btnPayment,btnRefresh,btnLogout})
            {
                button.Anchor=AnchorStyles.Top|AnchorStyles.Left;
                button.Margin=new Padding(4,10,0,0);
                actions.Controls.Add(button);
            }
            pnlTopBar.Controls.Add(actions);
            lblWelcome.AutoSize=false; lblWelcome.AutoEllipsis=true; lblWelcome.Size=new Size(285,32);
        }

        private async Task LoadAndRenderTablesAsync()
        {
            try
            {
                UseWaitCursor = true;
                flpTables.Controls.Clear();

                var tables = await Task.Run(() => _tableManagementService.GetTableDisplayItems());
                RenderTableCards(tables);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Masa listesi yuklenemedi.",
                    ex,
                    "TableManagementForm.LoadAndRenderTablesAsync");
            }
            finally
            {
                UseWaitCursor = false;
                await _notificationBar.RefreshAsync();
            }
        }

        private void RenderTableCards(List<TableDisplayItem> tableItems)
        {
            flpTables.SuspendLayout();

            foreach (var table in tableItems)
            {
                var card = CreateTableCard(table);
                flpTables.Controls.Add(card);
            }

            flpTables.ResumeLayout();
        }

        private Control CreateTableCard(TableDisplayItem table)
        {
            var isOccupied = table.IsOccupied;
            var backColor = isOccupied ? Color.FromArgb(34, 197, 94) : Color.Black;
            var borderColor = isOccupied ? Color.FromArgb(22, 163, 74) : Color.FromArgb(64, 64, 64);
            var textColor = Color.White;

            var panel = new Panel
            {
                Width = 270,
                Height = 164,
                BackColor = backColor,
                Margin = new Padding(12),
                Cursor = Cursors.Hand,
                Tag = table
            };

            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(borderColor, 2);
                e.Graphics.DrawRectangle(pen, 1, 1, panel.Width - 3, panel.Height - 3);
                using var accentBrush = new SolidBrush(borderColor);
                e.Graphics.FillRectangle(accentBrush, 1, 1, panel.Width - 2, 6);
            };
            panel.Click += TableCard_Click;

            var contextMenu = CreateTableContextMenu(table);
            panel.ContextMenuStrip = contextMenu;

            var lblName = new Label
            {
                AutoSize = false,
                Width = 240,
                Height = 32,
                Location = new Point(14, 18),
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                ForeColor = textColor,
                Text = table.TableName,
                Tag = table
            };
            lblName.Click += TableCard_Click;
            lblName.ContextMenuStrip = contextMenu;

            var lblStatus = new Label
            {
                AutoSize = false,
                Width = 240,
                Height = 24,
                Location = new Point(14, 64),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = textColor,
                Text = $"Durum: {(isOccupied ? "Dolu" : "Bos")}",
                Tag = table
            };
            lblStatus.Click += TableCard_Click;
            lblStatus.ContextMenuStrip = contextMenu;

            var totalText = isOccupied
                ? $"Toplam: {table.CurrentTotalAmount:C2}"
                : "Toplam: -";

            var lblTotal = new Label
            {
                AutoSize = false,
                Width = 240,
                Height = 24,
                Location = new Point(14, 96),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = textColor,
                Text = totalText,
                Tag = table
            };
            lblTotal.Click += TableCard_Click;
            lblTotal.ContextMenuStrip = contextMenu;

            var lblHint = new Label
            {
                AutoSize = false,
                Width = 240,
                Height = 20,
                Location = new Point(14, 128),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(229, 231, 235),
                Text = "Siparis icin tiklayin",
                Tag = table
            };
            lblHint.Click += TableCard_Click;
            lblHint.ContextMenuStrip = contextMenu;

            panel.Controls.Add(lblName);
            panel.Controls.Add(lblStatus);
            panel.Controls.Add(lblTotal);
            panel.Controls.Add(lblHint);
            return panel;
        }

        private ContextMenuStrip CreateTableContextMenu(TableDisplayItem table)
        {
            var menu = new ContextMenuStrip();
            var resetItem = new ToolStripMenuItem("Masayı Sıfırla");
            resetItem.Click += async (_, _) => await ResetTableFromContextAsync(table);
            menu.Items.Add(resetItem);
            return menu;
        }

        private async Task ResetTableFromContextAsync(TableDisplayItem table)
        {
            if (_isResettingTable)
            {
                return;
            }

            if (table.TableId <= 0)
            {
                MessageBox.Show(
                    "Geçerli bir masa bulunamadı.",
                    "Doğrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"{table.TableName} masası sıfırlansın mı?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _isResettingTable = true;
                UseWaitCursor = true;

                var result = await Task.Run(() => _paymentService.ResetTable(table.TableId));
                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message,
                        "İşlem Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(
                    result.Message,
                    "Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Masa durumu güncellenemedi.",
                    ex,
                    "TableManagementForm.ResetTableFromContextAsync");
            }
            finally
            {
                UseWaitCursor = false;
                _isResettingTable = false;
                await LoadAndRenderTablesAsync();
            }
        }

        private async void TableCard_Click(object? sender, EventArgs e)
        {
            if (_isOpeningTable)
            {
                return;
            }

            if ((Control.MouseButtons & MouseButtons.Right) == MouseButtons.Right)
            {
                return;
            }

            if (sender is not Control control || control.Tag is not TableDisplayItem table)
            {
                return;
            }

            if (table.TableId <= 0)
            {
                MessageBox.Show(
                    "Geçerli bir masa bulunamadı.",
                    "Dogrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _isOpeningTable = true;
                UseWaitCursor = true;
                var billId = await Task.Run(() => _tableManagementService.GetOrCreateOpenBillId(table.TableId, _loggedInUser.Id));

                using var orderForm = new OrderForm(
                    _loggedInUser,
                    table.TableId,
                    table.TableName,
                    billId,
                    _orderService,
                    _paymentService);
                orderForm.ShowDialog(this);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Islem Hatasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Masa acma islemi tamamlanamadi.",
                    ex,
                    "TableManagementForm.TableCard_Click",
                    "Islem Hatasi");
            }
            finally
            {
                UseWaitCursor = false;
                _isOpeningTable = false;
                await LoadAndRenderTablesAsync();
            }
        }

        private async void btnProducts_Click(object? sender, EventArgs e)
        {
            if (!IsAdminUser())
            {
                MessageBox.Show(
                    "Bu işlem için yönetici yetkisi gereklidir.",
                    "Yetki",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var form = new ProductManagementForm(_loggedInUser, _productManagementService);
                form.ShowDialog(this);
                await LoadAndRenderTablesAsync();
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Menü yönetimi ekranı açılamadı.",
                    ex,
                    "TableManagementForm.btnProducts_Click");
            }
        }

        private async void btnPayment_Click(object? sender, EventArgs e)
        {
            if(!IsAdminUser()) return;
            using var form=new RevenueForm(_loggedInUser,_revenueService);
            form.ShowDialog(this);
            await LoadAndRenderTablesAsync();
        }

        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            await LoadAndRenderTablesAsync();
        }

        private void btnLogout_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}

