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

            lblWelcome.Text = $"User: {fullName}";
            lblRole.Text = $"Role ID: {_loggedInUser.RoleId}";
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
                    $"Failed to load tables.\n\n{ex.Message}",
                    "Error",
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
            var backColor = isOccupied ? Color.FromArgb(236, 253, 245) : Color.FromArgb(239, 246, 255);
            var borderColor = isOccupied ? Color.FromArgb(22, 163, 74) : Color.FromArgb(37, 99, 235);

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
                ForeColor = Color.FromArgb(15, 23, 42),
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
                ForeColor = isOccupied ? Color.FromArgb(21, 128, 61) : Color.FromArgb(30, 64, 175),
                Text = $"Status: {(isOccupied ? "Occupied" : "Empty")}",
                Tag = table
            };
            lblStatus.Click += TableCard_Click;

            var totalText = isOccupied
                ? $"Total: {table.CurrentTotalAmount:C2}"
                : "Total: -";

            var lblTotal = new Label
            {
                AutoSize = false,
                Width = 240,
                Height = 24,
                Location = new Point(14, 96),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
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
                ForeColor = Color.FromArgb(100, 116, 139),
                Text = "Click to open orders",
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
                    $"Unable to open table.\n\n{ex.Message}",
                    "Operation Error",
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
                "Open payment from a table order to process a specific bill.",
                "Select a Bill",
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
