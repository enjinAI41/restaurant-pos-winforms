using System;
using System.ComponentModel;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class LoginForm : Form
    {
        private IAuthService? _authService;
        private ITableManagementService? _tableManagementService;
        private IOrderService? _orderService;
        private IPaymentService? _paymentService;
        private IProductManagementService? _productManagementService;

        public LoginForm()
        {
            InitializeComponent();
            CenterLoginPanel();

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                TryInitializeRuntimeServices();
            }
        }

        public LoginForm(
            IAuthService authService,
            ITableManagementService tableManagementService,
            IOrderService orderService,
            IPaymentService paymentService,
            IProductManagementService productManagementService)
            : this()
        {
            _authService = authService;
            _tableManagementService = tableManagementService;
            _orderService = orderService;
            _paymentService = paymentService;
            _productManagementService = productManagementService;
        }

        private void TryInitializeRuntimeServices()
        {
            if (_authService != null &&
                _tableManagementService != null &&
                _orderService != null &&
                _paymentService != null &&
                _productManagementService != null)
            {
                return;
            }

            var services = AppComposition.Create();
            _authService = services.AuthService;
            _tableManagementService = services.TableManagementService;
            _orderService = services.OrderService;
            _paymentService = services.PaymentService;
            _productManagementService = services.ProductManagementService;
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            try
            {
                var username = txtUsername.Text.Trim();
                var password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show(
                        "Kullanici adi ve sifre zorunludur.",
                        "Dogrulama",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (_authService == null)
                {
                    MessageBox.Show(
                        "Kimlik dogrulama servisi yapilandirilmamis.",
                        "Yapilandirma Hatasi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                User? loggedInUser = _authService.Authenticate(username, password);
                if (loggedInUser == null)
                {
                    MessageBox.Show(
                        "Kullanici adi veya sifre hatali.",
                        "Giris Basarisiz",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                OpenTableManagementForm(loggedInUser);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Giris sirasinda beklenmeyen bir hata olustu.\n\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void OpenTableManagementForm(User loggedInUser)
        {
            if (_tableManagementService == null || _orderService == null || _paymentService == null || _productManagementService == null)
            {
                MessageBox.Show(
                    "Uygulama servisleri yapilandirilmamis.",
                    "Yapilandirma Hatasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var tableManagementForm = new TableManagementForm(
                loggedInUser,
                _tableManagementService,
                _orderService,
                _paymentService,
                _productManagementService);
            tableManagementForm.FormClosed += (_, __) => Close();
            Hide();
            tableManagementForm.Show();
        }

        private void LoginForm_Resize(object? sender, EventArgs e)
        {
            CenterLoginPanel();
        }

        private void CenterLoginPanel()
        {
            if (pnlLoginCard == null)
            {
                return;
            }

            pnlLoginCard.Left = (ClientSize.Width - pnlLoginCard.Width) / 2;
            pnlLoginCard.Top = (ClientSize.Height - pnlLoginCard.Height) / 2;
        }
    }
}
