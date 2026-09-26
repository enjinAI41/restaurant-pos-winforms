using System;
using System.ComponentModel;
using System.Windows.Forms;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.WinForms.Infrastructure;

namespace Rivada.Restaurant.WinForms.Forms
{
    public partial class LoginForm : Form
    {
        private IAuthService? _authService;
        private ITableManagementService? _tableManagementService;
        private IOrderService? _orderService;
        private IPaymentService? _paymentService;
        private IProductManagementService? _productManagementService;
        private IStockService? _stockService;
        private INotificationService? _notificationService;
        private IRevenueService? _revenueService;

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
            IProductManagementService productManagementService,
            IStockService stockService,
            INotificationService notificationService,
            IRevenueService revenueService)
            : this()
        {
            _authService = authService;
            _tableManagementService = tableManagementService;
            _orderService = orderService;
            _paymentService = paymentService;
            _productManagementService = productManagementService;
            _stockService = stockService;
            _notificationService = notificationService;
            _revenueService = revenueService;
        }

        private void TryInitializeRuntimeServices()
        {
            if (_authService != null &&
                _tableManagementService != null &&
                _orderService != null &&
                _paymentService != null &&
                _productManagementService != null && _stockService != null && _notificationService != null && _revenueService != null)
            {
                return;
            }

            try
            {
                var services = AppComposition.Create();
                _authService = services.AuthService;
                _tableManagementService = services.TableManagementService;
                _orderService = services.OrderService;
                _paymentService = services.PaymentService;
                _productManagementService = services.ProductManagementService;
                _stockService = services.StockService;
                _notificationService = services.NotificationService;
                _revenueService = services.RevenueService;
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Uygulama servisleri baslatilamadi.",
                    ex,
                    "LoginForm.TryInitializeRuntimeServices",
                    "Baslatma Hatasi");
                btnLogin.Enabled = false;
            }
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
                        "Kullanici adi ve sifre bos birakilamaz.",
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
                        "Giris basarisiz. Kullanici bilgilerinizi kontrol edin.",
                        "Giris Basarisiz",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                OpenTableManagementForm(loggedInUser);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Islem Hatasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Giris islemi sirasinda beklenmeyen bir hata olustu.",
                    ex,
                    "LoginForm.btnLogin_Click");
            }
        }

        private void btnExit_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void OpenTableManagementForm(User loggedInUser)
        {
            if (_tableManagementService == null || _orderService == null || _paymentService == null || _productManagementService == null || _stockService == null || _notificationService == null || _revenueService == null)
            {
                MessageBox.Show(
                    "Uygulama servisleri hazir degil. Lutfen uygulamayi yeniden baslatin.",
                    "Yapilandirma Hatasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                var tableManagementForm = new TableManagementForm(
                    loggedInUser,
                    _tableManagementService,
                    _orderService,
                    _paymentService,
                    _productManagementService,
                    _stockService,
                    _notificationService,
                    _revenueService);
                tableManagementForm.FormClosed += (_, __) => Close();
                Hide();
                tableManagementForm.Show();
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    this,
                    "Masa yonetim ekrani acilamadi.",
                    ex,
                    "LoginForm.OpenTableManagementForm");
            }
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
