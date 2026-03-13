using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class TableManagementForm : Form
    {
        private readonly User _loggedInUser;
        private readonly ITableManagementService _tableManagementService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IProductManagementService _productManagementService;
        private bool _isOpeningTable;

        public TableManagementForm(
            User loggedInUser,
            ITableManagementService tableManagementService,
            IOrderService orderService,
            IPaymentService paymentService,
            IProductManagementService productManagementService)
        {
            _loggedInUser = loggedInUser ?? throw new ArgumentNullException(nameof(loggedInUser));
            _tableManagementService = tableManagementService ?? throw new ArgumentNullException(nameof(tableManagementService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _productManagementService = productManagementService ?? throw new ArgumentNullException(nameof(productManagementService));

            InitializeComponent();
            ConfigureVisualStyle();
            SetUserHeader();
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
            lblRole.Text = $"Rol ID: {_loggedInUser.RoleId}";
        }

        private void ConfigureVisualStyle()
        {
            flpTables.FlowDirection = FlowDirection.LeftToRight;
            flpTables.AutoScroll = true;
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
                MessageBox.Show(
                    $"Masalar yuklenemedi.\n\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
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

            panel.Controls.Add(lblName);
            panel.Controls.Add(lblStatus);
            panel.Controls.Add(lblTotal);
            panel.Controls.Add(lblHint);
            return panel;
        }

        private async void TableCard_Click(object? sender, EventArgs e)
        {
            if (_isOpeningTable)
            {
                return;
            }

            if (sender is not Control control || control.Tag is not TableDisplayItem table)
            {
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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Masa acilamadi.\n\n{ex.Message}",
                    "Islem Hatasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            using var form = new ProductManagementForm(_loggedInUser, _productManagementService);
            form.ShowDialog(this);
            await LoadAndRenderTablesAsync();
        }

        private async void btnPayment_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Belirli bir hesabi odemek icin masa siparis ekranindan odeme acin.",
                "Hesap Sec",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
