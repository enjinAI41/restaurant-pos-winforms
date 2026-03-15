#nullable disable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rivada.Restaurant.WinForms.Forms
{
    partial class TableManagementForm
    {
        private IContainer components = null!;
        private Panel pnlTopBar = null!;
        private Label lblWelcome = null!;
        private Label lblRole = null!;
        private FlowLayoutPanel flpTables = null!;
        private Button btnProducts = null!;
        private Button btnPayment = null!;
        private Button btnRefresh = null!;
        private Button btnLogout = null!;

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
            this.pnlTopBar = new Panel();
            this.lblWelcome = new Label();
            this.lblRole = new Label();
            this.btnProducts = new Button();
            this.btnPayment = new Button();
            this.btnRefresh = new Button();
            this.btnLogout = new Button();
            this.flpTables = new FlowLayoutPanel();
            this.pnlTopBar.SuspendLayout();
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(239, 244, 249);
            this.ClientSize = new Size(1220, 760);
            this.MinimumSize = new Size(1024, 680);
            this.Name = "TableManagementForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Rivada Restaurant - Masa Yonetimi";

            this.pnlTopBar.BackColor = Color.White;
            this.pnlTopBar.Dock = DockStyle.Top;
            this.pnlTopBar.Height = 92;
            this.pnlTopBar.Padding = new Padding(20, 14, 20, 14);

            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblWelcome.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblWelcome.Location = new Point(24, 18);
            this.lblWelcome.Text = "Hos geldiniz";

            this.lblRole.AutoSize = true;
            this.lblRole.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblRole.ForeColor = Color.FromArgb(100, 116, 139);
            this.lblRole.Location = new Point(26, 50);
            this.lblRole.Text = "Rol";

            ConfigureTopButton(this.btnProducts, "Menü Düzenle", 320);
            this.btnProducts.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.btnProducts.Size = new Size(128, 38);
            this.btnProducts.Click += new EventHandler(this.btnProducts_Click);

            ConfigureTopButton(this.btnPayment, "Kasa / Odeme", 835);
            this.btnPayment.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnPayment.Click += new EventHandler(this.btnPayment_Click);

            ConfigureTopButton(this.btnRefresh, "Yenile", 970);
            this.btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);

            ConfigureTopButton(this.btnLogout, "Cikis", 1080, true);
            this.btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnLogout.Click += new EventHandler(this.btnLogout_Click);

            this.flpTables.Dock = DockStyle.Fill;
            this.flpTables.AutoScroll = true;
            this.flpTables.Padding = new Padding(22, 20, 22, 20);
            this.flpTables.WrapContents = true;
            this.flpTables.BackColor = Color.FromArgb(239, 244, 249);

            this.pnlTopBar.Controls.Add(this.lblWelcome);
            this.pnlTopBar.Controls.Add(this.lblRole);
            this.pnlTopBar.Controls.Add(this.btnProducts);
            this.pnlTopBar.Controls.Add(this.btnPayment);
            this.pnlTopBar.Controls.Add(this.btnRefresh);
            this.pnlTopBar.Controls.Add(this.btnLogout);

            this.Controls.Add(this.flpTables);
            this.Controls.Add(this.pnlTopBar);

            this.pnlTopBar.ResumeLayout(false);
            this.pnlTopBar.PerformLayout();
            this.ResumeLayout(false);
        }

        private static void ConfigureTopButton(Button button, string text, int x, bool isDanger = false)
        {
            button.Text = text;
            button.Size = new Size(110, 38);
            button.Location = new Point(x, 27);
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            button.ForeColor = isDanger ? Color.FromArgb(153, 27, 27) : Color.FromArgb(15, 23, 42);
            button.BackColor = isDanger ? Color.FromArgb(254, 226, 226) : Color.FromArgb(226, 232, 240);
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
        }
    }
}


