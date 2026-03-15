using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Rivada.Restaurant.WinForms.Licensing;

namespace Rivada.Restaurant.WinForms.Forms
{
    public class LicenseForm : Form
    {
        private readonly OfflineLicenseService _licenseService;

        private readonly Label _lblStatusValue;
        private readonly Label _lblCompanyValue;
        private readonly Label _lblEndDateValue;
        private readonly TextBox _txtMachineCode;
        private readonly Label _lblMessage;

        public LicenseForm(OfflineLicenseService licenseService, LicenseValidationResult initialResult)
        {
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));

            Text = "Lisans Yönetimi";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 640;
            Height = 420;
            BackColor = Color.FromArgb(239, 244, 249);

            var title = new Label
            {
                Text = "Lisans Doğrulama",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(24, 18)
            };

            _lblMessage = new Label
            {
                AutoSize = false,
                Width = 580,
                Height = 44,
                Location = new Point(24, 52),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(153, 27, 27)
            };

            var panel = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(24, 102),
                Size = new Size(580, 210)
            };

            panel.Controls.Add(CreateLabel("Firma Adı:", 20, 24));
            _lblCompanyValue = CreateValueLabel("-", 180, 24, 360);
            panel.Controls.Add(_lblCompanyValue);

            panel.Controls.Add(CreateLabel("Cihaz Kodu:", 20, 62));
            _txtMachineCode = new TextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(180, 58),
                Size = new Size(255, 24),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Text = _licenseService.GetMachineCode()
            };
            panel.Controls.Add(_txtMachineCode);

            var btnCopy = new Button
            {
                Text = "Kopyala",
                Location = new Point(444, 56),
                Size = new Size(96, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += (_, _) => CopyMachineCode();
            panel.Controls.Add(btnCopy);

            panel.Controls.Add(CreateLabel("Lisans Durumu:", 20, 102));
            _lblStatusValue = CreateValueLabel("-", 180, 102, 360);
            panel.Controls.Add(_lblStatusValue);

            panel.Controls.Add(CreateLabel("Bitiş Tarihi:", 20, 140));
            _lblEndDateValue = CreateValueLabel("-", 180, 140, 360);
            panel.Controls.Add(_lblEndDateValue);

            var btnLoad = new Button
            {
                Text = "Lisans Yükle",
                Location = new Point(24, 328),
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(15, 118, 110),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += (_, _) => LoadLicense();

            var btnRetry = new Button
            {
                Text = "Tekrar Doğrula",
                Location = new Point(186, 328),
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 64, 175),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            btnRetry.FlatAppearance.BorderSize = 0;
            btnRetry.Click += (_, _) => Revalidate();

            var btnClose = new Button
            {
                Text = "Kapat",
                Location = new Point(454, 328),
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(153, 27, 27),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(title);
            Controls.Add(_lblMessage);
            Controls.Add(panel);
            Controls.Add(btnLoad);
            Controls.Add(btnRetry);
            Controls.Add(btnClose);

            ApplyValidationResult(initialResult);
        }

        private static Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(150, 24),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
        }

        private static Label CreateValueLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 24),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
        }

        private void CopyMachineCode()
        {
            try
            {
                Clipboard.SetText(_txtMachineCode.Text);
                MessageBox.Show("Cihaz kodu kopyalandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Cihaz kodu kopyalanamadı.", "İşlem Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Revalidate()
        {
            var result = _licenseService.ValidateCurrentLicense();
            ApplyValidationResult(result);
            if (result.IsValid)
            {
                MessageBox.Show("Lisans geçerli. Uygulamaya devam edebilirsiniz.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void LoadLicense()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Lisans Dosyası Seçiniz",
                Filter = "Lisans Dosyası (*.json;*.lic)|*.json;*.lic|Tüm Dosyalar (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var result = _licenseService.ImportLicense(dialog.FileName);
            ApplyValidationResult(result);

            if (result.IsValid)
            {
                MessageBox.Show("Lisans başarıyla yüklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            var fileName = Path.GetFileName(_licenseService.GetLicenseFilePath());
            MessageBox.Show(
                $"Lisans yükleme başarısız: {result.Message}\n\nDosya Yolu: {fileName}",
                "Lisans Hatası",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ApplyValidationResult(LicenseValidationResult result)
        {
            _txtMachineCode.Text = result.MachineCode;
            _lblStatusValue.Text = result.IsValid ? "Geçerli" : "Geçersiz";
            _lblStatusValue.ForeColor = result.IsValid
                ? Color.FromArgb(22, 101, 52)
                : Color.FromArgb(153, 27, 27);

            _lblCompanyValue.Text = result.License?.CompanyName ?? "-";
            _lblEndDateValue.Text = result.License != null
                ? result.License.EndDate.ToString("dd.MM.yyyy")
                : "-";
            _lblMessage.Text = result.Message;
        }
    }
}
