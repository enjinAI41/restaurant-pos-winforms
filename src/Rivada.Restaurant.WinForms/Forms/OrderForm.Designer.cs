#nullable disable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rivada.Restaurant.WinForms.Forms
{
    partial class OrderForm
    {
        private IContainer components = null!;
        private Panel pnlTop = null!;
        private Label lblTableTitle = null!;
        private TableLayoutPanel tlpMain = null!;
        private GroupBox grpCategories = null!;
        private GroupBox grpProducts = null!;
        private GroupBox grpBill = null!;
        private ListBox lstCategories = null!;
        private FlowLayoutPanel flpProducts = null!;
        private DataGridView dgvBillItems = null!;
        private Panel pnlBottom = null!;
        private Label lblSubtotalTitle = null!;
        private Label lblTotalTitle = null!;
        private Label lblSubtotalValue = null!;
        private Label lblTotalValue = null!;
        private Button btnBack = null!;
        private Button btnProceedPayment = null!;

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
            this.lblTableTitle = new Label();
            this.tlpMain = new TableLayoutPanel();
            this.grpCategories = new GroupBox();
            this.lstCategories = new ListBox();
            this.grpProducts = new GroupBox();
            this.flpProducts = new FlowLayoutPanel();
            this.grpBill = new GroupBox();
            this.dgvBillItems = new DataGridView();
            this.pnlBottom = new Panel();
            this.lblSubtotalTitle = new Label();
            this.lblSubtotalValue = new Label();
            this.lblTotalTitle = new Label();
            this.lblTotalValue = new Label();
            this.btnBack = new Button();
            this.btnProceedPayment = new Button();
            this.pnlTop.SuspendLayout();
            this.tlpMain.SuspendLayout();
            this.grpCategories.SuspendLayout();
            this.grpProducts.SuspendLayout();
            this.grpBill.SuspendLayout();
            ((ISupportInitialize)(this.dgvBillItems)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(239, 244, 249);
            this.ClientSize = new Size(1380, 800);
            this.MinimumSize = new Size(1200, 720);
            this.Name = "OrderForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Order";

            this.pnlTop.BackColor = Color.White;
            this.pnlTop.Dock = DockStyle.Top;
            this.pnlTop.Height = 82;
            this.pnlTop.Padding = new Padding(20, 18, 20, 18);

            this.lblTableTitle.AutoSize = true;
            this.lblTableTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTableTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblTableTitle.Location = new Point(20, 24);
            this.lblTableTitle.Text = "Table";

            this.tlpMain.Dock = DockStyle.Fill;
            this.tlpMain.Padding = new Padding(16, 16, 16, 12);
            this.tlpMain.ColumnCount = 3;
            this.tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            this.tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            this.tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            this.tlpMain.RowCount = 1;
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.grpCategories.Text = "Categories";
            this.grpCategories.Dock = DockStyle.Fill;
            this.grpCategories.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpCategories.BackColor = Color.White;
            this.grpCategories.Controls.Add(this.lstCategories);

            this.lstCategories.Dock = DockStyle.Fill;
            this.lstCategories.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lstCategories.IntegralHeight = false;
            this.lstCategories.BorderStyle = BorderStyle.FixedSingle;
            this.lstCategories.ItemHeight = 17;
            this.lstCategories.SelectedIndexChanged += new EventHandler(this.lstCategories_SelectedIndexChanged);

            this.grpProducts.Text = "Products";
            this.grpProducts.Dock = DockStyle.Fill;
            this.grpProducts.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpProducts.BackColor = Color.White;
            this.grpProducts.Controls.Add(this.flpProducts);

            this.flpProducts.Dock = DockStyle.Fill;
            this.flpProducts.AutoScroll = true;
            this.flpProducts.WrapContents = true;
            this.flpProducts.Padding = new Padding(12);
            this.flpProducts.BackColor = Color.White;

            this.grpBill.Text = "Current Bill";
            this.grpBill.Dock = DockStyle.Fill;
            this.grpBill.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpBill.BackColor = Color.White;
            this.grpBill.Controls.Add(this.dgvBillItems);

            this.dgvBillItems.Dock = DockStyle.Fill;
            this.dgvBillItems.AllowUserToAddRows = false;
            this.dgvBillItems.AllowUserToDeleteRows = false;
            this.dgvBillItems.AllowUserToResizeRows = false;
            this.dgvBillItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvBillItems.MultiSelect = false;
            this.dgvBillItems.ReadOnly = true;
            this.dgvBillItems.RowHeadersVisible = false;
            this.dgvBillItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvBillItems.BackgroundColor = Color.White;
            this.dgvBillItems.BorderStyle = BorderStyle.None;
            this.dgvBillItems.GridColor = Color.FromArgb(226, 232, 240);
            this.dgvBillItems.CellContentClick += new DataGridViewCellEventHandler(this.dgvBillItems_CellContentClick);

            this.pnlBottom.BackColor = Color.White;
            this.pnlBottom.Dock = DockStyle.Bottom;
            this.pnlBottom.Height = 94;

            this.lblSubtotalTitle.AutoSize = true;
            this.lblSubtotalTitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblSubtotalTitle.ForeColor = Color.FromArgb(51, 65, 85);
            this.lblSubtotalTitle.Location = new Point(22, 20);
            this.lblSubtotalTitle.Text = "Subtotal:";

            this.lblSubtotalValue.AutoSize = true;
            this.lblSubtotalValue.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblSubtotalValue.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblSubtotalValue.Location = new Point(94, 20);
            this.lblSubtotalValue.Text = "0.00";

            this.lblTotalTitle.AutoSize = true;
            this.lblTotalTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTotalTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblTotalTitle.Location = new Point(22, 50);
            this.lblTotalTitle.Text = "Total:";

            this.lblTotalValue.AutoSize = true;
            this.lblTotalValue.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTotalValue.ForeColor = Color.FromArgb(15, 118, 110);
            this.lblTotalValue.Location = new Point(84, 48);
            this.lblTotalValue.Text = "0.00";

            ConfigureFooterButton(this.btnBack, "Back", 980, false);
            this.btnBack.Click += new EventHandler(this.btnBack_Click);

            ConfigureFooterButton(this.btnProceedPayment, "Proceed to Payment", 1110, true);
            this.btnProceedPayment.Size = new Size(230, 44);
            this.btnProceedPayment.Click += new EventHandler(this.btnProceedPayment_Click);

            this.pnlTop.Controls.Add(this.lblTableTitle);

            this.grpCategories.Controls.Add(this.lstCategories);
            this.grpProducts.Controls.Add(this.flpProducts);
            this.grpBill.Controls.Add(this.dgvBillItems);

            this.tlpMain.Controls.Add(this.grpCategories, 0, 0);
            this.tlpMain.Controls.Add(this.grpProducts, 1, 0);
            this.tlpMain.Controls.Add(this.grpBill, 2, 0);

            this.pnlBottom.Controls.Add(this.lblSubtotalTitle);
            this.pnlBottom.Controls.Add(this.lblSubtotalValue);
            this.pnlBottom.Controls.Add(this.lblTotalTitle);
            this.pnlBottom.Controls.Add(this.lblTotalValue);
            this.pnlBottom.Controls.Add(this.btnBack);
            this.pnlBottom.Controls.Add(this.btnProceedPayment);

            this.Controls.Add(this.tlpMain);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlTop);

            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.tlpMain.ResumeLayout(false);
            this.grpCategories.ResumeLayout(false);
            this.grpProducts.ResumeLayout(false);
            this.grpBill.ResumeLayout(false);
            ((ISupportInitialize)(this.dgvBillItems)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        private static void ConfigureFooterButton(Button button, string text, int x, bool isPrimary)
        {
            button.Text = text;
            button.Location = new Point(x, 22);
            button.Size = new Size(110, 42);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            button.Cursor = Cursors.Hand;
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            if (isPrimary)
            {
                button.BackColor = Color.FromArgb(15, 118, 110);
                button.ForeColor = Color.White;
            }
            else
            {
                button.BackColor = Color.FromArgb(226, 232, 240);
                button.ForeColor = Color.FromArgb(15, 23, 42);
            }
        }
    }
}


