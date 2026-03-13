#nullable disable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rivada.Restaurant.WinForms.Forms
{
    partial class LoginForm
    {
        private IContainer components = null!;
        private Panel pnlLoginCard = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblUsername = null!;
        private Label lblPassword = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Button btnExit = null!;

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
            this.pnlLoginCard = new Panel();
            this.lblTitle = new Label();
            this.lblSubtitle = new Label();
            this.lblUsername = new Label();
            this.lblPassword = new Label();
            this.txtUsername = new TextBox();
            this.txtPassword = new TextBox();
            this.btnLogin = new Button();
            this.btnExit = new Button();
            this.pnlLoginCard.SuspendLayout();
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(239, 244, 249);
            this.ClientSize = new Size(960, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.Name = "LoginForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Rivada Restaurant Otomasyonu - Giris";
            this.Resize += new EventHandler(this.LoginForm_Resize);

            this.pnlLoginCard.BackColor = Color.White;
            this.pnlLoginCard.BorderStyle = BorderStyle.FixedSingle;
            this.pnlLoginCard.Size = new Size(430, 390);
            this.pnlLoginCard.Name = "pnlLoginCard";

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new Point(34, 28);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Restoran Girisi";

            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
            this.lblSubtitle.Location = new Point(36, 70);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Text = "Devam etmek icin giris yapin";

            this.lblUsername.AutoSize = true;
            this.lblUsername.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblUsername.ForeColor = Color.FromArgb(30, 41, 59);
            this.lblUsername.Location = new Point(36, 126);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Text = "Kullanici Adi";

            this.txtUsername.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtUsername.BorderStyle = BorderStyle.FixedSingle;
            this.txtUsername.Location = new Point(36, 150);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new Size(358, 25);
            this.txtUsername.TabIndex = 0;

            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblPassword.ForeColor = Color.FromArgb(30, 41, 59);
            this.lblPassword.Location = new Point(36, 198);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Text = "Sifre";

            this.txtPassword.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtPassword.BorderStyle = BorderStyle.FixedSingle;
            this.txtPassword.Location = new Point(36, 222);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '●';
            this.txtPassword.Size = new Size(358, 25);
            this.txtPassword.TabIndex = 1;

            this.btnLogin.BackColor = Color.FromArgb(15, 118, 110);
            this.btnLogin.FlatStyle = FlatStyle.Flat;
            this.btnLogin.FlatAppearance.BorderSize = 0;
            this.btnLogin.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.btnLogin.ForeColor = Color.White;
            this.btnLogin.Location = new Point(36, 286);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new Size(358, 40);
            this.btnLogin.TabIndex = 2;
            this.btnLogin.Text = "Giris Yap";
            this.btnLogin.UseVisualStyleBackColor = false;
            this.btnLogin.Click += new EventHandler(this.btnLogin_Click);

            this.btnExit.BackColor = Color.FromArgb(226, 232, 240);
            this.btnExit.FlatStyle = FlatStyle.Flat;
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.btnExit.ForeColor = Color.FromArgb(30, 41, 59);
            this.btnExit.Location = new Point(36, 332);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new Size(358, 34);
            this.btnExit.TabIndex = 3;
            this.btnExit.Text = "Cikis";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new EventHandler(this.btnExit_Click);

            this.AcceptButton = this.btnLogin;
            this.CancelButton = this.btnExit;

            this.pnlLoginCard.Controls.Add(this.lblTitle);
            this.pnlLoginCard.Controls.Add(this.lblSubtitle);
            this.pnlLoginCard.Controls.Add(this.lblUsername);
            this.pnlLoginCard.Controls.Add(this.txtUsername);
            this.pnlLoginCard.Controls.Add(this.lblPassword);
            this.pnlLoginCard.Controls.Add(this.txtPassword);
            this.pnlLoginCard.Controls.Add(this.btnLogin);
            this.pnlLoginCard.Controls.Add(this.btnExit);

            this.Controls.Add(this.pnlLoginCard);

            this.pnlLoginCard.ResumeLayout(false);
            this.pnlLoginCard.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}


