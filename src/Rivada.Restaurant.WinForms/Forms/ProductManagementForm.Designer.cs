#nullable disable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rivada.Restaurant.WinForms.Forms
{
    partial class ProductManagementForm
    {
        private IContainer components = null!;
        private Panel pnlTop = null!;
        private Label lblTitle = null!;
        private Label lblUser = null!;
        private SplitContainer splitMain = null!;
        private GroupBox grpCategories = null!;
        private ListBox lstCategories = null!;
        private Label lblCategoryName = null!;
        private TextBox txtCategoryName = null!;
        private CheckBox chkCategoryIsActive = null!;
        private Button btnCategoryAdd = null!;
        private Button btnCategoryUpdate = null!;
        private Button btnCategoryToggleActive = null!;
        private GroupBox grpProductForm = null!;
        private Label lblProductName = null!;
        private Label lblCategory = null!;
        private Label lblPrice = null!;
        private TextBox txtProductName = null!;
        private ComboBox cmbCategory = null!;
        private TextBox txtPrice = null!;
        private CheckBox chkIsActive = null!;
        private Button btnAdd = null!;
        private Button btnUpdate = null!;
        private Button btnProductClear = null!;
        private Button btnMarkInactive = null!;
        private Button btnDelete = null!;
        private DataGridView dgvProducts = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this.pnlTop = new Panel();
            this.lblTitle = new Label();
            this.lblUser = new Label();
            this.splitMain = new SplitContainer();
            this.grpCategories = new GroupBox();
            this.lstCategories = new ListBox();
            this.lblCategoryName = new Label();
            this.txtCategoryName = new TextBox();
            this.chkCategoryIsActive = new CheckBox();
            this.btnCategoryAdd = new Button();
            this.btnCategoryUpdate = new Button();
            this.btnCategoryToggleActive = new Button();
            this.grpProductForm = new GroupBox();
            this.lblProductName = new Label();
            this.lblCategory = new Label();
            this.lblPrice = new Label();
            this.txtProductName = new TextBox();
            this.cmbCategory = new ComboBox();
            this.txtPrice = new TextBox();
            this.chkIsActive = new CheckBox();
            this.btnAdd = new Button();
            this.btnUpdate = new Button();
            this.btnProductClear = new Button();
            this.btnMarkInactive = new Button();
            this.btnDelete = new Button();
            this.dgvProducts = new DataGridView();
            this.pnlTop.SuspendLayout();
            ((ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpCategories.SuspendLayout();
            this.grpProductForm.SuspendLayout();
            ((ISupportInitialize)(this.dgvProducts)).BeginInit();
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(239, 244, 249);
            this.ClientSize = new Size(1260, 760);
            this.MinimumSize = new Size(1120, 700);
            this.Name = "ProductManagementForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Menü Yönetimi";

            this.pnlTop.Dock = DockStyle.Top;
            this.pnlTop.Height = 78;
            this.pnlTop.BackColor = Color.White;

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new Point(20, 17);
            this.lblTitle.Text = "Menü Yönetimi";

            this.lblUser.AutoSize = true;
            this.lblUser.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblUser.ForeColor = Color.FromArgb(71, 85, 105);
            this.lblUser.Location = new Point(22, 48);
            this.lblUser.Text = "Kullanıcı";

            this.splitMain.Dock = DockStyle.Fill;
            this.splitMain.Location = new Point(0, 78);
            this.splitMain.Name = "splitMain";
            this.splitMain.SplitterDistance = 350;
            this.splitMain.Panel1MinSize = 120;
            this.splitMain.Panel2MinSize = 120;

            this.grpCategories.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.grpCategories.BackColor = Color.White;
            this.grpCategories.Text = "Kategoriler";
            this.grpCategories.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpCategories.Location = new Point(12, 12);
            this.grpCategories.Size = new Size(326, 658);

            this.lstCategories.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lstCategories.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lstCategories.Location = new Point(16, 32);
            this.lstCategories.Size = new Size(294, 292);
            this.lstCategories.SelectedIndexChanged += new EventHandler(this.lstCategories_SelectedIndexChanged);

            this.lblCategoryName.AutoSize = true;
            this.lblCategoryName.Text = "Kategori Adı";
            this.lblCategoryName.Location = new Point(16, 340);

            this.txtCategoryName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.txtCategoryName.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtCategoryName.BorderStyle = BorderStyle.FixedSingle;
            this.txtCategoryName.Location = new Point(16, 364);
            this.txtCategoryName.Size = new Size(294, 25);

            this.chkCategoryIsActive.AutoSize = true;
            this.chkCategoryIsActive.Text = "Aktif";
            this.chkCategoryIsActive.Checked = true;
            this.chkCategoryIsActive.Location = new Point(16, 399);

            ConfigureActionButton(this.btnCategoryAdd, "Kategori Ekle", 16, 430, 140, Color.FromArgb(15, 118, 110), Color.White);
            this.btnCategoryAdd.Click += new EventHandler(this.btnCategoryAdd_Click);

            ConfigureActionButton(this.btnCategoryUpdate, "Kategori Güncelle", 170, 430, 140, Color.FromArgb(30, 64, 175), Color.White);
            this.btnCategoryUpdate.Click += new EventHandler(this.btnCategoryUpdate_Click);

            ConfigureActionButton(this.btnCategoryToggleActive, "Kategoriyi Pasif Yap", 16, 476, 294, Color.FromArgb(254, 240, 138), Color.FromArgb(113, 63, 18));
            this.btnCategoryToggleActive.Click += new EventHandler(this.btnCategoryToggleActive_Click);

            this.grpProductForm.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.grpProductForm.Text = "Ürün Detayları";
            this.grpProductForm.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpProductForm.BackColor = Color.White;
            this.grpProductForm.Location = new Point(12, 12);
            this.grpProductForm.Size = new Size(874, 206);

            this.lblProductName.AutoSize = true;
            this.lblProductName.Text = "Ürün Adı";
            this.lblProductName.Location = new Point(20, 36);

            this.txtProductName.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtProductName.BorderStyle = BorderStyle.FixedSingle;
            this.txtProductName.Location = new Point(20, 60);
            this.txtProductName.Size = new Size(300, 25);

            this.lblCategory.AutoSize = true;
            this.lblCategory.Text = "Kategori";
            this.lblCategory.Location = new Point(336, 36);

            this.cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbCategory.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.cmbCategory.Location = new Point(336, 60);
            this.cmbCategory.Size = new Size(220, 25);

            this.lblPrice.AutoSize = true;
            this.lblPrice.Text = "Fiyat";
            this.lblPrice.Location = new Point(572, 36);

            this.txtPrice.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtPrice.BorderStyle = BorderStyle.FixedSingle;
            this.txtPrice.Location = new Point(572, 60);
            this.txtPrice.Size = new Size(150, 25);
            this.txtPrice.KeyPress += new KeyPressEventHandler(this.txtPrice_KeyPress);

            this.chkIsActive.AutoSize = true;
            this.chkIsActive.Text = "Aktif";
            this.chkIsActive.Location = new Point(742, 62);
            this.chkIsActive.Checked = true;

            ConfigureActionButton(this.btnAdd, "Ürün Ekle", 20, 108, 118, Color.FromArgb(15, 118, 110), Color.White);
            this.btnAdd.Click += new EventHandler(this.btnAdd_Click);

            ConfigureActionButton(this.btnUpdate, "Ürün Güncelle", 146, 108, 128, Color.FromArgb(30, 64, 175), Color.White);
            this.btnUpdate.Click += new EventHandler(this.btnUpdate_Click);

            ConfigureActionButton(this.btnProductClear, "Temizle", 282, 108, 100, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            this.btnProductClear.Click += new EventHandler(this.btnProductClear_Click);

            ConfigureActionButton(this.btnMarkInactive, "Ürünü Sil", 390, 108, 156, Color.FromArgb(254, 240, 138), Color.FromArgb(113, 63, 18));
            this.btnMarkInactive.Click += new EventHandler(this.btnMarkInactive_Click);

            ConfigureActionButton(this.btnDelete, "Ürünü Pasife Al", 554, 108, 156, Color.FromArgb(254, 226, 226), Color.FromArgb(153, 27, 27));
            this.btnDelete.Click += new EventHandler(this.btnDelete_Click);

            this.dgvProducts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.dgvProducts.Location = new Point(12, 232);
            this.dgvProducts.Size = new Size(874, 438);
            this.dgvProducts.ReadOnly = true;
            this.dgvProducts.AllowUserToAddRows = false;
            this.dgvProducts.AllowUserToDeleteRows = false;
            this.dgvProducts.AllowUserToResizeRows = false;
            this.dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvProducts.MultiSelect = false;
            this.dgvProducts.RowHeadersVisible = false;
            this.dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvProducts.BackgroundColor = Color.White;
            this.dgvProducts.BorderStyle = BorderStyle.None;
            this.dgvProducts.GridColor = Color.FromArgb(226, 232, 240);
            this.dgvProducts.CellClick += new DataGridViewCellEventHandler(this.dgvProducts_CellClick);

            this.pnlTop.Controls.Add(this.lblTitle);
            this.pnlTop.Controls.Add(this.lblUser);

            this.grpCategories.Controls.Add(this.lstCategories);
            this.grpCategories.Controls.Add(this.lblCategoryName);
            this.grpCategories.Controls.Add(this.txtCategoryName);
            this.grpCategories.Controls.Add(this.chkCategoryIsActive);
            this.grpCategories.Controls.Add(this.btnCategoryAdd);
            this.grpCategories.Controls.Add(this.btnCategoryUpdate);
            this.grpCategories.Controls.Add(this.btnCategoryToggleActive);

            this.grpProductForm.Controls.Add(this.lblProductName);
            this.grpProductForm.Controls.Add(this.txtProductName);
            this.grpProductForm.Controls.Add(this.lblCategory);
            this.grpProductForm.Controls.Add(this.cmbCategory);
            this.grpProductForm.Controls.Add(this.lblPrice);
            this.grpProductForm.Controls.Add(this.txtPrice);
            this.grpProductForm.Controls.Add(this.chkIsActive);
            this.grpProductForm.Controls.Add(this.btnAdd);
            this.grpProductForm.Controls.Add(this.btnUpdate);
            this.grpProductForm.Controls.Add(this.btnProductClear);
            this.grpProductForm.Controls.Add(this.btnMarkInactive);
            this.grpProductForm.Controls.Add(this.btnDelete);

            this.splitMain.Panel1.Controls.Add(this.grpCategories);
            this.splitMain.Panel2.Controls.Add(this.grpProductForm);
            this.splitMain.Panel2.Controls.Add(this.dgvProducts);

            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlTop);

            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.grpCategories.ResumeLayout(false);
            this.grpCategories.PerformLayout();
            this.grpProductForm.ResumeLayout(false);
            this.grpProductForm.PerformLayout();
            ((ISupportInitialize)(this.dgvProducts)).EndInit();
            this.ResumeLayout(false);
        }

        private static void ConfigureActionButton(Button button, string text, int x, int y, int width, Color backColor, Color foreColor)
        {
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 36);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            button.Cursor = Cursors.Hand;
        }
    }
}
