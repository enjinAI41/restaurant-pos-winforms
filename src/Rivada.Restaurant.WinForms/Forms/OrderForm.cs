using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;

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
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadBillItemsAsync();
        }

        private void UpdateHeader()
        {
            lblTableTitle.Text = $"Table: {_tableName}  |  Bill ID: {_billId}";
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
                HeaderText = "Product",
                FillWeight = 200
            };

            var colQty = new DataGridViewTextBoxColumn
            {
                Name = "colQuantity",
                HeaderText = "Qty",
                FillWeight = 70
            };

            var colUnitPrice = new DataGridViewTextBoxColumn
            {
                Name = "colUnitPrice",
                HeaderText = "Unit Price",
                FillWeight = 95
            };

            var colLineTotal = new DataGridViewTextBoxColumn
            {
                Name = "colLineTotal",
                HeaderText = "Line Total",
                FillWeight = 95
            };

            var colNote = new DataGridViewTextBoxColumn
            {
                Name = "colNote",
                HeaderText = "Note",
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
                HeaderText = "Note",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                FillWeight = 70,
                FlatStyle = FlatStyle.Flat
            };

            var colRemove = new DataGridViewButtonColumn
            {
                Name = "colRemove",
                HeaderText = "Remove",
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
                MessageBox.Show(
                    $"Could not load categories.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                MessageBox.Show(
                    $"Could not load products.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                return;
            }

            try
            {
                _isItemOperationRunning = true;
                UseWaitCursor = true;
                await Task.Run(() => _orderService.AddProductToBill(_billId, product.Id));
                await LoadBillItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not add product.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load bill details.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                    "Edit",
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

            var row = dgvBillItems.Rows[e.RowIndex];
            var billItemId = Convert.ToInt32(row.Cells["colId"].Value);
            var columnName = dgvBillItems.Columns[e.ColumnIndex].Name;

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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not update bill item.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

        private void btnProceedPayment_Click(object? sender, EventArgs e)
        {
            using var paymentForm = new PaymentForm(_loggedInUser, _billId, _tableId, _tableName, _paymentService);
            var result = paymentForm.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                Close();
            }
        }

        private static class NotePromptDialog
        {
            public static NoteDialogResult ShowDialog(string currentNote)
            {
                using var form = new Form
                {
                    Text = "Edit Item Note",
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(440, 220),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MinimizeBox = false,
                    MaximizeBox = false
                };

                var lbl = new Label
                {
                    Text = "Note:",
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
                    Text = "Save",
                    DialogResult = DialogResult.OK,
                    Location = new Point(246, 136),
                    Size = new Size(80, 32)
                };

                var btnCancel = new Button
                {
                    Text = "Cancel",
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
    }
}
