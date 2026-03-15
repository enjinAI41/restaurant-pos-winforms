using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.WinForms.Infrastructure;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class PaymentForm : Form
    {
        private readonly User _loggedInUser;
        private readonly int? _billId;
        private readonly int? _tableId;
        private readonly string _tableName;
        private readonly IPaymentService? _paymentService;
        private bool _isProcessingPayment;
        private bool _isResettingTable;

        public PaymentForm(User user)
            : this(user, null, null, null, null)
        {
        }

        public PaymentForm(User user, int? billId, int? tableId, string? tableName)
            : this(user, billId, tableId, tableName, null)
        {
        }

        public PaymentForm(
            User user,
            int? billId,
            int? tableId,
            string? tableName,
            IPaymentService? paymentService)
        {
            _loggedInUser = user ?? throw new ArgumentNullException(nameof(user));
            _billId = billId;
            _tableId = tableId;
            _tableName = tableName ?? "-";
            _paymentService = paymentService;

            InitializeComponent();
            ConfigureVisualStyle();
            ConfigureBillGrid();
            UpdateHeader();
        }

        private void ConfigureVisualStyle()
        {
            dgvBillItems.EnableHeadersVisualStyles = false;
            dgvBillItems.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(226, 232, 240);
            dgvBillItems.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            dgvBillItems.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            dgvBillItems.ColumnHeadersHeight = 38;
            dgvBillItems.RowTemplate.Height = 34;
            dgvBillItems.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            dgvBillItems.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            dgvBillItems.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await LoadPaymentDataAsync();
        }

        private void ConfigureBillGrid()
        {
            dgvBillItems.Columns.Clear();
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Urun", FillWeight = 180 });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "Adet", FillWeight = 60 });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Birim Fiyat", FillWeight = 90 });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "Satir Toplami", FillWeight = 90 });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNote", HeaderText = "Not", FillWeight = 130 });
        }

        private void UpdateHeader()
        {
            lblTable.Text = _billId.HasValue
                ? $"Masa: {_tableName} (ID: {_tableId})  |  Hesap ID: {_billId}"
                : "Hesap secilmedi";
        }

        private async Task LoadPaymentDataAsync()
        {
            if (!_billId.HasValue)
            {
                btnCompletePayment.Enabled = false;
                btnPaymentDone.Enabled = false;
                btnResetTable.Enabled = false;
                MessageBox.Show(
                    "Aktif adisyon bulunamadı.",
                    "Hesap Yok",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (_billId.Value <= 0 || !_tableId.HasValue || _tableId.Value <= 0)
            {
                btnCompletePayment.Enabled = false;
                btnPaymentDone.Enabled = false;
                btnResetTable.Enabled = false;
                MessageBox.Show(
                    "Geçerli bir masa bulunamadı.",
                    "Dogrulama",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_paymentService == null)
            {
                btnCompletePayment.Enabled = false;
                btnPaymentDone.Enabled = false;
                btnResetTable.Enabled = false;
                MessageBox.Show(
                    "Odeme servisi yapilandirilmamis.",
                    "Yapilandirma Hatasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                UseWaitCursor = true;
                var viewData = await Task.Run(() => _paymentService.GetPaymentViewData(_billId.Value, _tableName));

                dgvBillItems.Rows.Clear();
                foreach (var item in viewData.Items)
                {
                    dgvBillItems.Rows.Add(
                        item.ProductNameSnapshot,
                        item.Quantity,
                        item.UnitPrice.ToString("C2", CultureInfo.CurrentCulture),
                        item.LineTotal.ToString("C2", CultureInfo.CurrentCulture),
                        item.Note ?? string.Empty);
                }

                lblSubtotalValue.Text = viewData.Subtotal.ToString("C2", CultureInfo.CurrentCulture);
                lblTotalValue.Text = viewData.Total.ToString("C2", CultureInfo.CurrentCulture);
                txtReceiptPreview.Text = viewData.ReceiptPreview;
                btnPaymentDone.Enabled = true;
                btnResetTable.Enabled = true;
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
                    "Odeme verileri yuklenemedi.",
                    ex,
                    "PaymentForm.LoadPaymentDataAsync");
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private async void btnCompletePayment_Click(object? sender, EventArgs e)
        {
            await CompletePaymentAsync();
        }

        private async void btnPaymentDone_Click(object? sender, EventArgs e)
        {
            await CompletePaymentAsync();
        }

        private async void btnResetTable_Click(object? sender, EventArgs e)
        {
            if (_isResettingTable)
            {
                return;
            }

            if (!_tableId.HasValue || _paymentService == null)
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
                _isResettingTable = true;
                btnResetTable.Enabled = false;
                btnCompletePayment.Enabled = false;
                btnPaymentDone.Enabled = false;
                UseWaitCursor = true;

                var result = await Task.Run(() => _paymentService.ResetTable(_tableId.Value));
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

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Masa durumu güncellenemedi.",
                    ex,
                    "PaymentForm.btnResetTable_Click");
            }
            finally
            {
                UseWaitCursor = false;
                _isResettingTable = false;
                btnResetTable.Enabled = true;
                btnCompletePayment.Enabled = true;
                btnPaymentDone.Enabled = true;
            }
        }

        private async Task CompletePaymentAsync()
        {
            if (_isProcessingPayment)
            {
                return;
            }

            if (!_billId.HasValue || _paymentService == null)
            {
                return;
            }

            try
            {
                _isProcessingPayment = true;
                btnCompletePayment.Enabled = false;
                btnPaymentDone.Enabled = false;
                UseWaitCursor = true;

                var paymentType = rdoCard.Checked ? PaymentType.Card : PaymentType.Cash;
                var result = await Task.Run(() => _paymentService.CompletePayment(_billId.Value, _loggedInUser.Id, paymentType));

                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message,
                        "Odeme Tamamlanmadi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    await LoadPaymentDataAsync();
                    return;
                }

                MessageBox.Show(
                    result.Message,
                    "Basarili",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
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
                    "Ödeme işlemi tamamlanamadı.",
                    ex,
                    "PaymentForm.CompletePaymentAsync");
            }
            finally
            {
                UseWaitCursor = false;
                _isProcessingPayment = false;
                btnCompletePayment.Enabled = true;
                btnPaymentDone.Enabled = true;
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}

