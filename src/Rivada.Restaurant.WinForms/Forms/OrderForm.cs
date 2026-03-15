using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;
using Rivada.Restaurant.WinForms.Infrastructure;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class OrderForm : Form
    {
        private readonly User _loggedInUser;
        private readonly int _tableId;
        private readonly string _tableName;
        private readonly int _billId;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private bool _isItemOperationRunning;
        private bool _isResetOperationRunning;

        public OrderForm(
            User loggedInUser,
            int tableId,
            string tableName,
            int billId,
            IOrderService orderService,
            IPaymentService paymentService)
        {
            _loggedInUser = loggedInUser ?? throw new ArgumentNullException(nameof(loggedInUser));
            _tableId = tableId;
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _billId = billId;
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));

            InitializeComponent();
            ConfigureVisualStyle();
            ConfigureBillGrid();
            UpdateHeader();
            ConfigureFooterLayout();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            if (_billId <= 0 || _tableId <= 0)
            {
                MessageBox.Show(
                    "Geçerli bir masa bulunamadı.",
                    "Dogrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
                return;
            }

            await LoadCategoriesAsync();
            await LoadBillItemsAsync();
        }

        private void UpdateHeader()
        {
            lblTableTitle.Text = $"Masa: {_tableName}  |  Hesap ID: {_billId}";
        }

        private void ConfigureVisualStyle()
        {
            flpProducts.FlowDirection = FlowDirection.LeftToRight;
            flpProducts.WrapContents = true;

            dgvBillItems.EnableHeadersVisualStyles = false;
            dgvBillItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(226, 232, 240);
            dgvBillItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgvBillItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            dgvBillItems.ColumnHeadersHeight = 38;
            dgvBillItems.RowTemplate.Height = 34;
            dgvBillItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvBillItems.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvBillItems.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        }

        private void ConfigureFooterLayout()
        {
            btnProceedPayment.Visible = true;
            btnResetTable.Visible = true;
            btnProceedPayment.BringToFront();
            btnResetTable.BringToFront();
            btnBack.BringToFront();

            pnlBottom.Resize += (_, _) => LayoutFooterButtons();
            LayoutFooterButtons();
        }

        private void LayoutFooterButtons()
        {
            const int rightMargin = 22;
            const int spacing = 12;
            const int top = 22;

            btnProceedPayment.Top = top;
            btnProceedPayment.Left = Math.Max(
                rightMargin,
                pnlBottom.ClientSize.Width - btnProceedPayment.Width - rightMargin);

            btnBack.Top = top;
            btnBack.Left = Math.Max(
                rightMargin,
                btnProceedPayment.Left - btnResetTable.Width - btnBack.Width - (spacing * 2));

            btnResetTable.Top = top;
            btnResetTable.Left = Math.Max(
                rightMargin,
                btnProceedPayment.Left - btnResetTable.Width - spacing);
        }

        private void ConfigureBillGrid()
        {
            dgvBillItems.Columns.Clear();

            var colId = new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "Id",
                Visible = false
            };

            var colName = new DataGridViewTextBoxColumn
            {
                Name = "colProductName",
                HeaderText = "Urun",
                FillWeight = 200
            };

            var colQty = new DataGridViewTextBoxColumn
            {
                Name = "colQuantity",
                HeaderText = "Adet",
                FillWeight = 70
            };

            var colUnitPrice = new DataGridViewTextBoxColumn
            {
                Name = "colUnitPrice",
                HeaderText = "Birim Fiyat",
                FillWeight = 95
            };

            var colLineTotal = new DataGridViewTextBoxColumn
            {
                Name = "colLineTotal",
                HeaderText = "Satir Toplami",
                FillWeight = 95
            };

            var colNote = new DataGridViewTextBoxColumn
            {
                Name = "colNote",
                HeaderText = "Not",
                FillWeight = 145
            };

            var colPlus = new DataGridViewButtonColumn
            {
                Name = "colPlus",
                HeaderText = "+",
                Text = "+",
                UseColumnTextForButtonValue = true,
                FillWeight = 45,
                FlatStyle = FlatStyle.Flat
            };

            var colMinus = new DataGridViewButtonColumn
            {
                Name = "colMinus",
                HeaderText = "-",
                Text = "-",
                UseColumnTextForButtonValue = true,
                FillWeight = 45,
                FlatStyle = FlatStyle.Flat
            };

            var colEditNote = new DataGridViewButtonColumn
            {
                Name = "colEditNote",
                HeaderText = "Not",
                Text = "Duzenle",
                UseColumnTextForButtonValue = true,
                FillWeight = 70,
                FlatStyle = FlatStyle.Flat
            };

            var colRemove = new DataGridViewButtonColumn
            {
                Name = "colRemove",
                HeaderText = "Sil",
                Text = "X",
                UseColumnTextForButtonValue = true,
                FillWeight = 55,
                FlatStyle = FlatStyle.Flat
            };

            dgvBillItems.Columns.AddRange(
                colId,
                colName,
                colQty,
                colUnitPrice,
                colLineTotal,
                colNote,
                colPlus,
                colMinus,
                colEditNote,
                colRemove);

            foreach (DataGridViewColumn column in dgvBillItems.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                UseWaitCursor = true;
                var categories = await Task.Run(() => _orderService.GetCategories());

                lstCategories.DataSource = categories;
                lstCategories.DisplayMember = "Name";
                lstCategories.ValueMember = "Id";

                if (lstCategories.Items.Count > 0)
                {
                    lstCategories.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Kategoriler yuklenemedi.",
                    ex,
                    "OrderForm.LoadCategoriesAsync");
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private async void lstCategories_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstCategories.SelectedValue is not int categoryId)
            {
                return;
            }

            await LoadProductsAsync(categoryId);
        }

        private async Task LoadProductsAsync(int categoryId)
        {
            try
            {
                UseWaitCursor = true;
                flpProducts.Controls.Clear();

                var products = await Task.Run(() => _orderService.GetProductsByCategory(categoryId));
                foreach (var product in products)
                {
                    flpProducts.Controls.Add(CreateProductButton(product));
                }
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Urun listesi yuklenemedi.",
                    ex,
                    "OrderForm.LoadProductsAsync");
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private Control CreateProductButton(Product product)
        {
            var button = new Button
            {
                Width = 190,
                Height = 96,
                Margin = new Padding(10),
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Text = $"{product.Name}{Environment.NewLine}{product.Price.ToString("C2", CultureInfo.CurrentCulture)}",
                Tag = product,
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(219, 234, 254);
            button.Click += ProductButton_Click;
            return button;
        }

        private async void ProductButton_Click(object? sender, EventArgs e)
        {
            if (_isItemOperationRunning)
            {
                return;
            }

            if (sender is not Button button || button.Tag is not Product product)
            {
                MessageBox.Show(
                    "Lütfen önce bir ürün seçin.",
                    "Doğrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _isItemOperationRunning = true;
                UseWaitCursor = true;
                await Task.Run(() => _orderService.AddProductToBill(_billId, product.Id));
                await LoadBillItemsAsync();
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
                    "Urun hesaba eklenemedi.",
                    ex,
                    "OrderForm.ProductButton_Click");
            }
            finally
            {
                UseWaitCursor = false;
                _isItemOperationRunning = false;
            }
        }

        private async Task LoadBillItemsAsync()
        {
            try
            {
                UseWaitCursor = true;
                var details = await Task.Run(() => _orderService.GetBillDetails(_billId));
                RenderBillItems(details);
                UpdateTotals(details);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "İşlem Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Hesap detaylari yuklenemedi.",
                    ex,
                    "OrderForm.LoadBillItemsAsync");
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void RenderBillItems(BillDetails billDetails)
        {
            dgvBillItems.Rows.Clear();

            foreach (var item in billDetails.Items)
            {
                dgvBillItems.Rows.Add(
                    item.Id,
                    item.ProductNameSnapshot,
                    item.Quantity,
                    item.UnitPrice.ToString("C2", CultureInfo.CurrentCulture),
                    item.LineTotal.ToString("C2", CultureInfo.CurrentCulture),
                    item.Note ?? string.Empty,
                    "+",
                    "-",
                    "Duzenle",
                    "X");
            }
        }

        private void UpdateTotals(BillDetails billDetails)
        {
            lblSubtotalValue.Text = billDetails.Subtotal.ToString("C2", CultureInfo.CurrentCulture);
            lblTotalValue.Text = billDetails.Total.ToString("C2", CultureInfo.CurrentCulture);
        }

        private async void dgvBillItems_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isItemOperationRunning)
            {
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var columnName = dgvBillItems.Columns[e.ColumnIndex].Name;
            if (columnName != "colPlus" && columnName != "colMinus" && columnName != "colRemove" && columnName != "colEditNote")
            {
                return;
            }

            if (!TryGetBillItemIdFromRow(e.RowIndex, out var billItemId))
            {
                MessageBox.Show(
                    "Seçili sipariş satırı bulunamadı.",
                    "Doğrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var operationErrorMessage = "Sipariş güncellenemedi.";
            try
            {
                _isItemOperationRunning = true;
                UseWaitCursor = true;

                if (columnName == "colPlus")
                {
                    await Task.Run(() => _orderService.IncreaseBillItemQuantity(billItemId));
                }
                else if (columnName == "colMinus")
                {
                    await Task.Run(() => _orderService.DecreaseBillItemQuantity(billItemId));
                }
                else if (columnName == "colRemove")
                {
                    await Task.Run(() => _orderService.RemoveBillItem(billItemId));
                }
                else if (columnName == "colEditNote")
                {
                    operationErrorMessage = "Sipariş notu kaydedilemedi.";
                    var row = dgvBillItems.Rows[e.RowIndex];
                    var currentNote = Convert.ToString(row.Cells["colNote"].Value);
                    var noteResult = NotePromptDialog.ShowDialog(currentNote ?? string.Empty);
                    if (!noteResult.Submitted)
                    {
                        return;
                    }

                    await Task.Run(() => _orderService.UpdateBillItemNote(billItemId, noteResult.Note));
                }

                await LoadBillItemsAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "İşlem Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    operationErrorMessage,
                    ex,
                    "OrderForm.dgvBillItems_CellContentClick");
            }
            finally
            {
                UseWaitCursor = false;
                _isItemOperationRunning = false;
            }
        }

        private void btnBack_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private async void btnResetTable_Click(object? sender, EventArgs e)
        {
            if (_isResetOperationRunning)
            {
                return;
            }

            if (_tableId <= 0)
            {
                MessageBox.Show(
                    "Geçerli bir masa bulunamadı.",
                    "Doğrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Bu masa sıfırlansın mı?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _isResetOperationRunning = true;
                btnResetTable.Enabled = false;
                UseWaitCursor = true;

                var result = await Task.Run(() => _paymentService.ResetTable(_tableId));
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
                Close();
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Masa durumu güncellenemedi.",
                    ex,
                    "OrderForm.btnResetTable_Click");
            }
            finally
            {
                UseWaitCursor = false;
                _isResetOperationRunning = false;
                btnResetTable.Enabled = true;
            }
        }

        private void btnProceedPayment_Click(object? sender, EventArgs e)
        {
            if (_billId <= 0 || _tableId <= 0)
            {
                MessageBox.Show(
                    "Aktif adisyon bulunamadı.",
                    "Dogrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var paymentForm = new PaymentForm(_loggedInUser, _billId, _tableId, _tableName, _paymentService);
                var result = paymentForm.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Odeme ekrani acilamadi.",
                    ex,
                    "OrderForm.btnProceedPayment_Click");
            }
        }

        private static class NotePromptDialog
        {
            public static NoteDialogResult ShowDialog(string currentNote)
            {
                using var form = new Form
                {
                    Text = "Kalem Notunu Duzenle",
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(440, 220),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MinimizeBox = false,
                    MaximizeBox = false
                };

                var lbl = new Label
                {
                    Text = "Not:",
                    AutoSize = true,
                    Location = new Point(20, 22)
                };

                var txt = new TextBox
                {
                    Multiline = true,
                    Size = new Size(390, 82),
                    Location = new Point(20, 44),
                    Text = currentNote
                };

                var btnSave = new Button
                {
                    Text = "Kaydet",
                    DialogResult = DialogResult.OK,
                    Location = new Point(246, 136),
                    Size = new Size(80, 32)
                };

                var btnCancel = new Button
                {
                    Text = "Iptal",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(330, 136),
                    Size = new Size(80, 32)
                };

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btnSave);
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnSave;
                form.CancelButton = btnCancel;

                var dialogResult = form.ShowDialog();
                return new NoteDialogResult
                {
                    Submitted = dialogResult == DialogResult.OK,
                    Note = dialogResult == DialogResult.OK ? txt.Text : null
                };
            }
        }

        private class NoteDialogResult
        {
            public bool Submitted { get; set; }
            public string? Note { get; set; }
        }

        private bool TryGetBillItemIdFromRow(int rowIndex, out int billItemId)
        {
            billItemId = 0;
            if (rowIndex < 0 || rowIndex >= dgvBillItems.Rows.Count)
            {
                return false;
            }

            var row = dgvBillItems.Rows[rowIndex];
            if (row?.Cells["colId"].Value == null)
            {
                return false;
            }

            return int.TryParse(Convert.ToString(row.Cells["colId"].Value), out billItemId) && billItemId > 0;
        }
    }
}

