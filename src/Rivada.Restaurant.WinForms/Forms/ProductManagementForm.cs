using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;
using Rivada.Restaurant.WinForms.Infrastructure;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class ProductManagementForm : Form
    {
        private readonly User _loggedInUser;
        private readonly IProductManagementService _productManagementService;
        private int _selectedProductId;
        private int _selectedCategoryId;
        private bool _isBusy;
        private bool _hasCategories;
        private List<ProductListItem> _allProducts = new();

        public ProductManagementForm(User user, IProductManagementService productManagementService)
        {
            _loggedInUser = user ?? throw new ArgumentNullException(nameof(user));
            _productManagementService = productManagementService ?? throw new ArgumentNullException(nameof(productManagementService));

            InitializeComponent();
            ConfigureVisualStyle();
            ConfigureGrid();
            SetUserInfo();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (!IsAdminUser())
            {
                MessageBox.Show(
                    "Bu işlem için admin yetkisi gereklidir.",
                    "Yetki",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
                return;
            }

            await LoadInitialDataAsync();
        }

        private bool IsAdminUser()
        {
            return string.Equals(_loggedInUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private void ConfigureVisualStyle()
        {
            dgvProducts.EnableHeadersVisualStyles = false;
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(226, 232, 240);
            dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            dgvProducts.ColumnHeadersHeight = 38;
            dgvProducts.RowTemplate.Height = 34;
            dgvProducts.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            dgvProducts.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            dgvProducts.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
        }

        private void SetUserInfo()
        {
            var fullName = string.IsNullOrWhiteSpace(_loggedInUser.FullName)
                ? _loggedInUser.Username
                : _loggedInUser.FullName;

            lblUser.Text = $"Kullanıcı: {fullName}";
        }

        private void ConfigureGrid()
        {
            dgvProducts.Columns.Clear();
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "Id", Visible = false });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Ürün Adı", FillWeight = 220 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Kategori", FillWeight = 160 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Fiyat", FillWeight = 90 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPriceRaw", HeaderText = "PriceRaw", Visible = false });
            dgvProducts.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colIsActive", HeaderText = "Aktif", FillWeight = 70 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategoryId", HeaderText = "CategoryId", Visible = false });

            foreach (DataGridViewColumn column in dgvProducts.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private async Task LoadInitialDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadProductsAsync();
            ClearProductForm();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                UseWaitCursor = true;
                var categories = await Task.Run(() => _productManagementService.GetCategories(false));
                _hasCategories = categories.Count > 0;

                lstCategories.DataSource = null;
                lstCategories.DataSource = categories;
                lstCategories.DisplayMember = "Name";
                lstCategories.ValueMember = "Id";

                var previousId = _selectedCategoryId;
                cmbCategory.DataSource = null;
                cmbCategory.DataSource = categories;
                cmbCategory.DisplayMember = "Name";
                cmbCategory.ValueMember = "Id";

                if (_hasCategories)
                {
                    var categoryToSelect = categories.FirstOrDefault(x => x.Id == previousId) ?? categories[0];
                    _selectedCategoryId = categoryToSelect.Id;
                    lstCategories.SelectedValue = categoryToSelect.Id;
                    cmbCategory.SelectedValue = categoryToSelect.Id;
                    FillCategoryForm(categoryToSelect);
                }
                else
                {
                    _selectedCategoryId = 0;
                    ClearCategoryForm();
                }

                SetActionButtonsEnabled(_hasCategories);
            }
            catch (Exception ex)
            {
                _hasCategories = false;
                SetActionButtonsEnabled(false);
                AppDiagnostics.ShowError(
                    this,
                    "Kategori bilgisi yüklenemedi.",
                    ex,
                    "ProductManagementForm.LoadCategoriesAsync");
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                UseWaitCursor = true;
                _allProducts = await Task.Run(() => _productManagementService.GetProducts());
                RenderProducts();
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
                    "Ürünler yüklenemedi.",
                    ex,
                    "ProductManagementForm.LoadProductsAsync");
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void RenderProducts()
        {
            dgvProducts.Rows.Clear();

            IEnumerable<ProductListItem> products = _allProducts;
            if (_selectedCategoryId > 0)
            {
                products = products.Where(x => x.CategoryId == _selectedCategoryId);
            }

            foreach (var product in products)
            {
                dgvProducts.Rows.Add(
                    product.Id,
                    product.ProductName,
                    product.CategoryName,
                    product.Price.ToString("C2", CultureInfo.CurrentCulture),
                    product.Price.ToString(CultureInfo.InvariantCulture),
                    product.IsActive,
                    product.CategoryId);
            }
        }

        private async void btnCategoryAdd_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            var categoryName = txtCategoryName.Text.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Kategori adı boş bırakılamaz.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCategoryName.Focus();
                return;
            }

            try
            {
                _isBusy = true;
                await Task.Run(() => _productManagementService.AddCategory(categoryName, chkCategoryIsActive.Checked));
                MessageBox.Show("Kategori başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadCategoriesAsync();
                await LoadProductsAsync();
                SelectCategoryByName(categoryName);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Kategori kaydedilemedi.",
                    ex,
                    "ProductManagementForm.btnCategoryAdd_Click");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void btnCategoryUpdate_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            if (_selectedCategoryId <= 0)
            {
                MessageBox.Show("Güncellenecek kategori seçilmedi.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var categoryName = txtCategoryName.Text.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Kategori adı boş bırakılamaz.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCategoryName.Focus();
                return;
            }

            try
            {
                _isBusy = true;
                await Task.Run(() => _productManagementService.UpdateCategory(_selectedCategoryId, categoryName, chkCategoryIsActive.Checked));
                MessageBox.Show("Kategori başarıyla güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadCategoriesAsync();
                await LoadProductsAsync();
                SelectCategoryById(_selectedCategoryId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Kategori güncellenemedi.",
                    ex,
                    "ProductManagementForm.btnCategoryUpdate_Click");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void btnCategoryToggleActive_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            if (_selectedCategoryId <= 0)
            {
                MessageBox.Show("Güncellenecek kategori seçilmedi.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var makeActive = !chkCategoryIsActive.Checked;
            var actionText = makeActive ? "aktif" : "pasif";
            var confirm = MessageBox.Show(
                $"Seçili kategori {actionText} yapılsın mı?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _isBusy = true;
                await Task.Run(() => _productManagementService.SetCategoryActive(_selectedCategoryId, makeActive));
                MessageBox.Show(
                    makeActive ? "Kategori aktif yapıldı." : "Kategori pasif yapıldı.",
                    "Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await LoadCategoriesAsync();
                await LoadProductsAsync();
                SelectCategoryById(_selectedCategoryId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Kategori durumu güncellenemedi.",
                    ex,
                    "ProductManagementForm.btnCategoryToggleActive_Click");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void btnAdd_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            try
            {
                _isBusy = true;
                if (!TryReadProductForm(out var name, out var categoryId, out var price, out var isActive))
                {
                    return;
                }

                await Task.Run(() => _productManagementService.AddProduct(name, categoryId, price, isActive));
                MessageBox.Show("Ürün başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                ClearProductForm();
                SelectCategoryById(categoryId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Ürün kaydedilemedi.",
                    ex,
                    "ProductManagementForm.btnAdd_Click");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void btnUpdate_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            try
            {
                _isBusy = true;
                if (_selectedProductId <= 0)
                {
                    MessageBox.Show("Güncellenecek ürün seçilmedi.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!TryReadProductForm(out var name, out var categoryId, out var price, out var isActive))
                {
                    return;
                }

                await Task.Run(() => _productManagementService.UpdateProduct(_selectedProductId, name, categoryId, price, isActive));
                MessageBox.Show("Ürün başarıyla güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                SelectProductById(_selectedProductId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Ürün güncellenemedi.",
                    ex,
                    "ProductManagementForm.btnUpdate_Click");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void btnMarkInactive_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            if (_selectedProductId <= 0)
            {
                MessageBox.Show("Güncellenecek ürün seçilmedi.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Seçili ürün silinsin mi?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _isBusy = true;
                await Task.Run(() => _productManagementService.DeleteProduct(_selectedProductId));
                MessageBox.Show("Ürün başarıyla silindi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                ClearProductForm();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Ürün silinemedi.",
                    ex,
                    "ProductManagementForm.btnMarkInactive_Click");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void btnDelete_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            if (_selectedProductId <= 0)
            {
                MessageBox.Show("Güncellenecek ürün seçilmedi.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryGetSelectedProductValues(out var name, out var categoryId, out var price, out var currentIsActive))
            {
                MessageBox.Show("Seçili ürün bilgisi okunamadı.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!currentIsActive)
            {
                MessageBox.Show("Ürün zaten pasif durumda.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                "Seçili ürün pasife alınsın mı?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _isBusy = true;
                await Task.Run(() => _productManagementService.MarkProductInactive(_selectedProductId));
                MessageBox.Show("Ürün pasif duruma alındı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                SelectProductById(_selectedProductId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Ürün pasif duruma alınamadı.",
                    ex,
                    "ProductManagementForm.btnDelete_Click");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void btnProductClear_Click(object? sender, EventArgs e)
        {
            ClearProductForm();
        }

        private void lstCategories_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstCategories.SelectedItem is not Category category)
            {
                _selectedCategoryId = 0;
                RenderProducts();
                return;
            }

            _selectedCategoryId = category.Id;
            FillCategoryForm(category);

            if (_hasCategories)
            {
                cmbCategory.SelectedValue = category.Id;
            }

            RenderProducts();
            ClearProductForm();
        }

        private void dgvProducts_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var row = dgvProducts.Rows[e.RowIndex];
            if (row == null || row.Cells["colId"].Value == null || !int.TryParse(Convert.ToString(row.Cells["colId"].Value), out var selectedId) || selectedId <= 0)
            {
                MessageBox.Show("Güncellenecek ürün seçilmedi.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedProductId = selectedId;

            txtProductName.Text = Convert.ToString(row.Cells["colName"].Value) ?? string.Empty;
            txtPrice.Text = Convert.ToString(row.Cells["colPriceRaw"].Value) ?? string.Empty;
            chkIsActive.Checked = row.Cells["colIsActive"].Value is bool isActive && isActive;

            if (!int.TryParse(Convert.ToString(row.Cells["colCategoryId"].Value), out var categoryId) || categoryId <= 0)
            {
                _selectedProductId = 0;
                MessageBox.Show("Kategori bilgisi yüklenemedi.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cmbCategory.SelectedValue = categoryId;
            SelectCategoryById(categoryId);
        }

        private void txtPrice_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                if (txtPrice.Text.Contains('.') || txtPrice.Text.Contains(','))
                {
                    e.Handled = true;
                }

                return;
            }

            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private bool TryReadProductForm(out string name, out int categoryId, out decimal price, out bool isActive)
        {
            name = txtProductName.Text.Trim();
            categoryId = 0;
            price = 0;
            isActive = chkIsActive.Checked;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Ürün adı boş bırakılamaz.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtProductName.Focus();
                return false;
            }

            if (!_hasCategories || cmbCategory.Items.Count == 0)
            {
                MessageBox.Show("Kategori seçiniz.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return false;
            }

            if (cmbCategory.SelectedValue is not int selectedCategoryId || selectedCategoryId <= 0)
            {
                MessageBox.Show("Kategori seçiniz.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return false;
            }

            var normalizedPriceText = txtPrice.Text.Trim();
            if (!decimal.TryParse(normalizedPriceText, NumberStyles.Number, CultureInfo.CurrentCulture, out price) &&
                !decimal.TryParse(normalizedPriceText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price))
            {
                MessageBox.Show("Geçerli bir fiyat girin.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return false;
            }

            if (price <= 0)
            {
                MessageBox.Show("Fiyat sıfırdan büyük olmalıdır.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return false;
            }

            categoryId = selectedCategoryId;
            return true;
        }

        private bool TryGetSelectedProductValues(out string name, out int categoryId, out decimal price, out bool isActive)
        {
            name = txtProductName.Text.Trim();
            categoryId = 0;
            price = 0;
            isActive = chkIsActive.Checked;

            if (_selectedProductId <= 0)
            {
                return false;
            }

            if (cmbCategory.SelectedValue is not int selectedCategoryId || selectedCategoryId <= 0)
            {
                return false;
            }

            var normalizedPriceText = txtPrice.Text.Trim();
            if (!decimal.TryParse(normalizedPriceText, NumberStyles.Number, CultureInfo.CurrentCulture, out price) &&
                !decimal.TryParse(normalizedPriceText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price))
            {
                return false;
            }

            categoryId = selectedCategoryId;
            return !string.IsNullOrWhiteSpace(name) && price > 0;
        }

        private void FillCategoryForm(Category category)
        {
            txtCategoryName.Text = category.Name;
            chkCategoryIsActive.Checked = category.IsActive;
            btnCategoryToggleActive.Text = category.IsActive ? "Kategoriyi Pasif Yap" : "Kategoriyi Aktif Yap";
        }

        private void ClearCategoryForm()
        {
            txtCategoryName.Clear();
            chkCategoryIsActive.Checked = true;
            btnCategoryToggleActive.Text = "Kategoriyi Pasif Yap";
        }

        private void ClearProductForm()
        {
            _selectedProductId = 0;
            txtProductName.Clear();
            txtPrice.Clear();
            chkIsActive.Checked = true;

            if (_hasCategories && cmbCategory.Items.Count > 0)
            {
                if (_selectedCategoryId > 0)
                {
                    cmbCategory.SelectedValue = _selectedCategoryId;
                }
                else
                {
                    cmbCategory.SelectedIndex = 0;
                }
            }

            dgvProducts.ClearSelection();
            txtProductName.Focus();
        }

        private void SelectCategoryById(int categoryId)
        {
            if (categoryId <= 0 || lstCategories.Items.Count == 0)
            {
                return;
            }

            lstCategories.SelectedValue = categoryId;
        }

        private void SelectCategoryByName(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName) || lstCategories.Items.Count == 0)
            {
                return;
            }

            foreach (var item in lstCategories.Items)
            {
                if (item is Category category && string.Equals(category.Name, categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    lstCategories.SelectedItem = item;
                    return;
                }
            }
        }

        private void SelectProductById(int productId)
        {
            if (productId <= 0)
            {
                return;
            }

            foreach (DataGridViewRow row in dgvProducts.Rows)
            {
                if (row.Cells["colId"].Value == null)
                {
                    continue;
                }

                if (int.TryParse(Convert.ToString(row.Cells["colId"].Value), out var rowId) && rowId == productId)
                {
                    row.Selected = true;
                    dgvProducts.CurrentCell = row.Cells["colName"];
                    dgvProducts_CellClick(dgvProducts, new DataGridViewCellEventArgs(1, row.Index));
                    return;
                }
            }
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            btnAdd.Enabled = enabled;
            btnUpdate.Enabled = enabled;
            btnMarkInactive.Enabled = enabled;
            btnDelete.Enabled = enabled;
            btnProductClear.Enabled = enabled;
            btnCategoryUpdate.Enabled = enabled;
            btnCategoryToggleActive.Enabled = enabled;
        }
    }
}
