using System.Security.Cryptography;
using System.Text;

namespace Rivada.Restaurant.WinForms.Forms;

internal sealed class StockPasswordForm : Form
{
    private static readonly byte[] PasswordHash = Convert.FromHexString(
        "133806CA086FA756E78E210C0ACA10BE245BD2F8AD9544CB599E355020BBD59B");
    private readonly TextBox _password = new()
    {
        Name = "StockPassword", UseSystemPasswordChar = true, MaxLength = 64,
        Dock = DockStyle.Fill, AccessibleName = "Stok yönetimi şifresi"
    };
    private readonly Label _error = new()
    {
        Name = "PasswordError", AutoSize = true, ForeColor = Color.FromArgb(185, 28, 28),
        Margin = new Padding(0, 10, 0, 14)
    };

    public StockPasswordForm()
    {
        Name = "StockPasswordForm";
        Text = "Stok Yönetimi — Şifre";
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 230);
        BackColor = Color.FromArgb(239, 244, 249);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Text = "Stok yönetimine girmek için şifreyi yazın.", AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
        }, 0, 0);
        layout.Controls.Add(_password, 0, 1);
        layout.Controls.Add(_error, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var open = new Button { Name = "UnlockStock", Text = "Giriş", Size = new Size(105, 36) };
        var cancel = new Button { Name = "CancelStock", Text = "İptal", Size = new Size(105, 36), DialogResult = DialogResult.Cancel };
        open.Click += (_, _) => ValidatePassword();
        buttons.Controls.AddRange(new Control[] { open, cancel });
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
        AcceptButton = open;
        CancelButton = cancel;
        Shown += (_, _) => _password.Focus();
    }

    private void ValidatePassword()
    {
        var enteredHash = SHA256.HashData(Encoding.UTF8.GetBytes(_password.Text));
        var accepted = CryptographicOperations.FixedTimeEquals(PasswordHash, enteredHash);
        CryptographicOperations.ZeroMemory(enteredHash);
        _password.Clear();
        if (accepted)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }
        _error.Text = "Şifre yanlış. Tekrar deneyin.";
        _password.Focus();
    }
}
