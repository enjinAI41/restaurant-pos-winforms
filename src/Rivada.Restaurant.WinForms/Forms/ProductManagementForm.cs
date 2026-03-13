using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class ProductManagementForm : Form
    {
        private readonly User _loggedInUser;
        private readonly IProductManagementService _productManagementService;
        private int _selectedProductId;
        private bool _isBusy;

        public ProductManagementForm(User user, IProductManagementService productManagementService)
        {
            _loggedInUser = user ?? throw new ArgumentNullException(nameof(user));
            _productManagementService = productManagementService ?? throw new ArgumentNullException(nameof(productManagementService));

            InitializeComponent();
            ConfigureVisualStyle();
            ConfigureGrid();
            SetUserInfo();
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

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await LoadInitialDataAsync();
        }

        private void SetUserInfo()
        {
            var fullName = string.IsNullOrWhiteSpace(_loggedInUser.FullName)
                ? _loggedInUser.Username
                : _loggedInUser.FullName;

            lblUser.Text = $"User: {fullName}";
        }

        private void ConfigureGrid()
        {
            dgvProducts.Columns.Clear();
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "Id", Visible = false });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Product Name", FillWeight = 200 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Category", FillWeight = 140 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Price", FillWeight = 90 });
            dgvProducts.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colIsActive", HeaderText = "IsActive", FillWeight = 70 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategoryId", HeaderText = "CategoryId", Visible = false });
        }

        private async Task LoadInitialDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadProductsAsync();
            ClearForm();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                UseWaitCursor = true;
                var categories = await Task.Run(() => _productManagementService.GetCategories());
                cmbCategory.DataSource = categories;
                cmbCategory.DisplayMember = "Name";
                cmbCategory.ValueMember = "Id";
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

        private async Task LoadProductsAsync()
        {
            try
            {
                UseWaitCursor = true;
                var products = await Task.Run(() => _productManagementService.GetProducts());
                RenderProducts(products);
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

        private void RenderProducts(List<ProductListItem> products)
        {
            dgvProducts.Rows.Clear();

            foreach (var product in products)
            {
                dgvProducts.Rows.Add(
                    product.Id,
                    product.ProductName,
                    product.CategoryName,
                    product.Price.ToString("C2", CultureInfo.CurrentCulture),
                    product.IsActive,
                    product.CategoryId);
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
                if (!TryReadForm(out var name, out var categoryId, out var price, out var isActive))
                {
                    return;
                }

                await Task.Run(() => _productManagementService.AddProduct(name, categoryId, price, isActive));
                MessageBox.Show("Product added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                ClearForm();
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
                    MessageBox.Show("Select a product to update.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!TryReadForm(out var name, out var categoryId, out var price, out var isActive))
                {
                    return;
                }

                await Task.Run(() => _productManagementService.UpdateProduct(_selectedProductId, name, categoryId, price, isActive));
                MessageBox.Show("Product updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not update product.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void btnClear_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private async void btnMarkInactive_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            if (_selectedProductId <= 0)
            {
                MessageBox.Show("Select a product first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Mark selected product as inactive?",
                "Confirm",
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
                MessageBox.Show("Product marked inactive.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not mark product inactive.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                MessageBox.Show("Select a product first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Delete selected product permanently?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _isBusy = true;
                await Task.Run(() => _productManagementService.DeleteProduct(_selectedProductId));
                MessageBox.Show("Product deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadProductsAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not delete product.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void dgvProducts_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var row = dgvProducts.Rows[e.RowIndex];
            _selectedProductId = Convert.ToInt32(row.Cells["colId"].Value);

            txtProductName.Text = Convert.ToString(row.Cells["colName"].Value) ?? string.Empty;
            txtPrice.Text = Convert.ToString(row.Cells["colPrice"].Value) ?? string.Empty;
            chkIsActive.Checked = Convert.ToBoolean(row.Cells["colIsActive"].Value);

            var categoryId = Convert.ToInt32(row.Cells["colCategoryId"].Value);
            cmbCategory.SelectedValue = categoryId;
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

        private bool TryReadForm(out string name, out int categoryId, out decimal price, out bool isActive)
        {
            name = txtProductName.Text.Trim();
            categoryId = 0;
            price = 0;
            isActive = chkIsActive.Checked;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Product name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtProductName.Focus();
                return false;
            }

            if (cmbCategory.SelectedValue is not int selectedCategoryId || selectedCategoryId <= 0)
            {
                MessageBox.Show("Please select a category.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return false;
            }

            var normalizedPriceText = txtPrice.Text.Trim();
            if (!decimal.TryParse(normalizedPriceText, NumberStyles.Number, CultureInfo.CurrentCulture, out price) &&
                !decimal.TryParse(normalizedPriceText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price))
            {
                MessageBox.Show("Enter a valid price greater than 0.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return false;
            }

            if (price <= 0)
            {
                MessageBox.Show("Enter a valid price greater than 0.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return false;
            }

            categoryId = selectedCategoryId;
            return true;
        }

        private void ClearForm()
        {
            _selectedProductId = 0;
            txtProductName.Clear();
            txtPrice.Clear();
            chkIsActive.Checked = true;

            if (cmbCategory.Items.Count > 0)
            {
                cmbCategory.SelectedIndex = 0;
            }

            dgvProducts.ClearSelection();
            txtProductName.Focus();
        }
    }
}
