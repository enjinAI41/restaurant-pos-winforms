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
        private Button btnClear = null!;
        private Button btnDelete = null!;
        private Button btnMarkInactive = null!;
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
            this.btnClear = new Button();
            this.btnDelete = new Button();
            this.btnMarkInactive = new Button();
            this.dgvProducts = new DataGridView();
            this.pnlTop.SuspendLayout();
            this.grpProductForm.SuspendLayout();
            ((ISupportInitialize)(this.dgvProducts)).BeginInit();
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(239, 244, 249);
            this.ClientSize = new Size(1180, 760);
            this.MinimumSize = new Size(1060, 700);
            this.Name = "ProductManagementForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Product Management";

            this.pnlTop.Dock = DockStyle.Top;
            this.pnlTop.Height = 78;
            this.pnlTop.BackColor = Color.White;

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new Point(20, 17);
            this.lblTitle.Text = "Product Management";

            this.lblUser.AutoSize = true;
            this.lblUser.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblUser.ForeColor = Color.FromArgb(71, 85, 105);
            this.lblUser.Location = new Point(22, 48);
            this.lblUser.Text = "User";

            this.grpProductForm.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.grpProductForm.Text = "Product Details";
            this.grpProductForm.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpProductForm.BackColor = Color.White;
            this.grpProductForm.Location = new Point(16, 92);
            this.grpProductForm.Size = new Size(1148, 182);

            this.lblProductName.AutoSize = true;
            this.lblProductName.Text = "Product Name";
            this.lblProductName.Location = new Point(20, 38);

            this.txtProductName.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtProductName.BorderStyle = BorderStyle.FixedSingle;
            this.txtProductName.Location = new Point(20, 62);
            this.txtProductName.Size = new Size(330, 25);

            this.lblCategory.AutoSize = true;
            this.lblCategory.Text = "Category";
            this.lblCategory.Location = new Point(368, 38);

            this.cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbCategory.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.cmbCategory.Location = new Point(368, 62);
            this.cmbCategory.Size = new Size(250, 25);

            this.lblPrice.AutoSize = true;
            this.lblPrice.Text = "Price";
            this.lblPrice.Location = new Point(636, 38);

            this.txtPrice.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtPrice.BorderStyle = BorderStyle.FixedSingle;
            this.txtPrice.Location = new Point(636, 62);
            this.txtPrice.Size = new Size(160, 25);
            this.txtPrice.KeyPress += new KeyPressEventHandler(this.txtPrice_KeyPress);

            this.chkIsActive.AutoSize = true;
            this.chkIsActive.Text = "Is Active";
            this.chkIsActive.Location = new Point(820, 64);
            this.chkIsActive.Checked = true;

            ConfigureActionButton(this.btnAdd, "Add", 20, Color.FromArgb(15, 118, 110), Color.White);
            this.btnAdd.Click += new EventHandler(this.btnAdd_Click);

            ConfigureActionButton(this.btnUpdate, "Update", 146, Color.FromArgb(30, 64, 175), Color.White);
            this.btnUpdate.Click += new EventHandler(this.btnUpdate_Click);

            ConfigureActionButton(this.btnClear, "Clear", 272, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            this.btnClear.Click += new EventHandler(this.btnClear_Click);

            ConfigureActionButton(this.btnMarkInactive, "Mark Inactive", 398, Color.FromArgb(254, 240, 138), Color.FromArgb(113, 63, 18));
            this.btnMarkInactive.Click += new EventHandler(this.btnMarkInactive_Click);

            ConfigureActionButton(this.btnDelete, "Delete", 560, Color.FromArgb(254, 226, 226), Color.FromArgb(153, 27, 27));
            this.btnDelete.Click += new EventHandler(this.btnDelete_Click);

            this.dgvProducts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.dgvProducts.Location = new Point(16, 290);
            this.dgvProducts.Size = new Size(1148, 454);
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

            this.grpProductForm.Controls.Add(this.lblProductName);
            this.grpProductForm.Controls.Add(this.txtProductName);
            this.grpProductForm.Controls.Add(this.lblCategory);
            this.grpProductForm.Controls.Add(this.cmbCategory);
            this.grpProductForm.Controls.Add(this.lblPrice);
            this.grpProductForm.Controls.Add(this.txtPrice);
            this.grpProductForm.Controls.Add(this.chkIsActive);
            this.grpProductForm.Controls.Add(this.btnAdd);
            this.grpProductForm.Controls.Add(this.btnUpdate);
            this.grpProductForm.Controls.Add(this.btnClear);
            this.grpProductForm.Controls.Add(this.btnMarkInactive);
            this.grpProductForm.Controls.Add(this.btnDelete);

            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.grpProductForm);
            this.Controls.Add(this.dgvProducts);

            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.grpProductForm.ResumeLayout(false);
            this.grpProductForm.PerformLayout();
            ((ISupportInitialize)(this.dgvProducts)).EndInit();
            this.ResumeLayout(false);
        }

        private static void ConfigureActionButton(Button button, string text, int x, Color backColor, Color foreColor)
        {
            button.Text = text;
            button.Location = new Point(x, 116);
            button.Size = new Size(text == "Mark Inactive" ? 150 : 118, 38);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            button.Cursor = Cursors.Hand;
        }
    }
}


