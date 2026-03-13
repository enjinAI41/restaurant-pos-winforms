#nullable disable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rivada.Restaurant.WinForms.Forms
{
    partial class PaymentForm
    {
        private IContainer components = null!;
        private Panel pnlTop = null!;
        private Label lblTitle = null!;
        private Label lblTable = null!;
        private DataGridView dgvBillItems = null!;
        private Panel pnlBottom = null!;
        private GroupBox grpPaymentMethod = null!;
        private RadioButton rdoCash = null!;
        private RadioButton rdoCard = null!;
        private Label lblSubtotalTitle = null!;
        private Label lblSubtotalValue = null!;
        private Label lblTotalTitle = null!;
        private Label lblTotalValue = null!;
        private Button btnCompletePayment = null!;
        private Button btnCancel = null!;
        private GroupBox grpReceipt = null!;
        private TextBox txtReceiptPreview = null!;

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
            this.lblTable = new Label();
            this.dgvBillItems = new DataGridView();
            this.pnlBottom = new Panel();
            this.grpPaymentMethod = new GroupBox();
            this.rdoCash = new RadioButton();
            this.rdoCard = new RadioButton();
            this.lblSubtotalTitle = new Label();
            this.lblSubtotalValue = new Label();
            this.lblTotalTitle = new Label();
            this.lblTotalValue = new Label();
            this.btnCompletePayment = new Button();
            this.btnCancel = new Button();
            this.grpReceipt = new GroupBox();
            this.txtReceiptPreview = new TextBox();
            this.pnlTop.SuspendLayout();
            ((ISupportInitialize)(this.dgvBillItems)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.grpPaymentMethod.SuspendLayout();
            this.grpReceipt.SuspendLayout();
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(239, 244, 249);
            this.ClientSize = new Size(1160, 760);
            this.MinimumSize = new Size(1040, 680);
            this.Name = "PaymentForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Kasa / Odeme";

            this.pnlTop.Dock = DockStyle.Top;
            this.pnlTop.Height = 80;
            this.pnlTop.BackColor = Color.White;

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new Point(20, 18);
            this.lblTitle.Text = "Odemeyi Tamamla";

            this.lblTable.AutoSize = true;
            this.lblTable.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblTable.ForeColor = Color.FromArgb(71, 85, 105);
            this.lblTable.Location = new Point(22, 50);
            this.lblTable.Text = "Masa";

            this.dgvBillItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            this.dgvBillItems.Location = new Point(18, 92);
            this.dgvBillItems.Size = new Size(730, 520);
            this.dgvBillItems.ReadOnly = true;
            this.dgvBillItems.AllowUserToAddRows = false;
            this.dgvBillItems.AllowUserToDeleteRows = false;
            this.dgvBillItems.AllowUserToResizeRows = false;
            this.dgvBillItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvBillItems.MultiSelect = false;
            this.dgvBillItems.RowHeadersVisible = false;
            this.dgvBillItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvBillItems.BackgroundColor = Color.White;
            this.dgvBillItems.BorderStyle = BorderStyle.None;
            this.dgvBillItems.GridColor = Color.FromArgb(226, 232, 240);

            this.grpReceipt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            this.grpReceipt.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpReceipt.Text = "Fis Onizleme";
            this.grpReceipt.Location = new Point(766, 92);
            this.grpReceipt.Size = new Size(376, 520);
            this.grpReceipt.BackColor = Color.White;

            this.txtReceiptPreview.Multiline = true;
            this.txtReceiptPreview.ReadOnly = true;
            this.txtReceiptPreview.ScrollBars = ScrollBars.Vertical;
            this.txtReceiptPreview.Dock = DockStyle.Fill;
            this.txtReceiptPreview.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtReceiptPreview.BackColor = Color.White;

            this.pnlBottom.Dock = DockStyle.Bottom;
            this.pnlBottom.Height = 132;
            this.pnlBottom.BackColor = Color.White;

            this.grpPaymentMethod.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpPaymentMethod.Text = "Odeme Yontemi";
            this.grpPaymentMethod.Location = new Point(18, 12);
            this.grpPaymentMethod.Size = new Size(290, 106);
            this.grpPaymentMethod.BackColor = Color.White;

            this.rdoCash.AutoSize = true;
            this.rdoCash.Location = new Point(18, 32);
            this.rdoCash.Text = "Nakit";
            this.rdoCash.Checked = true;

            this.rdoCard.AutoSize = true;
            this.rdoCard.Location = new Point(18, 62);
            this.rdoCard.Text = "Kart";

            this.lblSubtotalTitle.AutoSize = true;
            this.lblSubtotalTitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblSubtotalTitle.ForeColor = Color.FromArgb(71, 85, 105);
            this.lblSubtotalTitle.Location = new Point(330, 34);
            this.lblSubtotalTitle.Text = "Ara Toplam:";

            this.lblSubtotalValue.AutoSize = true;
            this.lblSubtotalValue.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblSubtotalValue.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblSubtotalValue.Location = new Point(398, 33);
            this.lblSubtotalValue.Text = "0.00";

            this.lblTotalTitle.AutoSize = true;
            this.lblTotalTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTotalTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblTotalTitle.Location = new Point(330, 66);
            this.lblTotalTitle.Text = "Toplam:";

            this.lblTotalValue.AutoSize = true;
            this.lblTotalValue.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTotalValue.ForeColor = Color.FromArgb(15, 118, 110);
            this.lblTotalValue.Location = new Point(389, 63);
            this.lblTotalValue.Text = "0.00";

            this.btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnCancel.Location = new Point(836, 42);
            this.btnCancel.Size = new Size(130, 42);
            this.btnCancel.Text = "Iptal";
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.BackColor = Color.FromArgb(226, 232, 240);
            this.btnCancel.ForeColor = Color.FromArgb(15, 23, 42);
            this.btnCancel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);

            this.btnCompletePayment.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnCompletePayment.Location = new Point(976, 42);
            this.btnCompletePayment.Size = new Size(166, 42);
            this.btnCompletePayment.Text = "Odemeyi Tamamla";
            this.btnCompletePayment.FlatStyle = FlatStyle.Flat;
            this.btnCompletePayment.FlatAppearance.BorderSize = 0;
            this.btnCompletePayment.BackColor = Color.FromArgb(15, 118, 110);
            this.btnCompletePayment.ForeColor = Color.White;
            this.btnCompletePayment.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.btnCompletePayment.Click += new EventHandler(this.btnCompletePayment_Click);

            this.pnlTop.Controls.Add(this.lblTitle);
            this.pnlTop.Controls.Add(this.lblTable);

            this.grpPaymentMethod.Controls.Add(this.rdoCash);
            this.grpPaymentMethod.Controls.Add(this.rdoCard);
            this.grpReceipt.Controls.Add(this.txtReceiptPreview);

            this.pnlBottom.Controls.Add(this.grpPaymentMethod);
            this.pnlBottom.Controls.Add(this.lblSubtotalTitle);
            this.pnlBottom.Controls.Add(this.lblSubtotalValue);
            this.pnlBottom.Controls.Add(this.lblTotalTitle);
            this.pnlBottom.Controls.Add(this.lblTotalValue);
            this.pnlBottom.Controls.Add(this.btnCancel);
            this.pnlBottom.Controls.Add(this.btnCompletePayment);

            this.Controls.Add(this.dgvBillItems);
            this.Controls.Add(this.grpReceipt);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlTop);

            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            ((ISupportInitialize)(this.dgvBillItems)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.grpPaymentMethod.ResumeLayout(false);
            this.grpPaymentMethod.PerformLayout();
            this.grpReceipt.ResumeLayout(false);
            this.grpReceipt.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}


