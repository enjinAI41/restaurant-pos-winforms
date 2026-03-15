using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.WinForms.Forms;
using Rivada.Restaurant.WinForms.Infrastructure;
using Rivada.Restaurant.WinForms.Licensing;

namespace Rivada.Restaurant.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            RegisterGlobalExceptionHandlers();

            var licenseService = CreateLicenseService();
            if (TryHandleToolArguments(args, licenseService))
            {
                return;
            }

            try
            {
                var licenseResult = licenseService.ValidateCurrentLicense();
                if (!licenseResult.IsValid)
                {
                    using var licenseForm = new LicenseForm(licenseService, licenseResult);
                    if (licenseForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    owner: null,
                    userMessage: "Lisans doğrulama sırasında beklenmeyen bir hata oluştu.",
                    ex: ex,
                    context: "Program.Main.ValidateLicense",
                    title: "Lisans Hatası");
                return;
            }

            LoginForm loginForm;
            try
            {
                loginForm = CreateLoginForm();
            }
            catch (Exception ex)
            {
                AppDiagnostics.ShowError(
                    owner: null,
                    userMessage: "Uygulama baslatilamadi. Veritabani veya servisler hazir degil.",
                    ex: ex,
                    context: "Program.Main.CreateLoginForm",
                    title: "Baslatma Hatasi");
                return;
            }

            Application.Run(loginForm);
        }

        private static OfflineLicenseService CreateLicenseService()
        {
            var licensePath = Path.Combine(AppContext.BaseDirectory, "license.json");
            var statePath = Path.Combine(AppContext.BaseDirectory, "license.state.json");
            return new OfflineLicenseService(licensePath, statePath);
        }

        private static bool TryHandleToolArguments(string[] args, OfflineLicenseService licenseService)
        {
            if (args == null || args.Length == 0)
            {
                return false;
            }

            try
            {
                if (string.Equals(args[0], "--cihaz-kodu", StringComparison.OrdinalIgnoreCase))
                {
                    var machineCode = licenseService.GetMachineCode();
                    MessageBox.Show(
                        $"Cihaz Kodu:\n{machineCode}",
                        "Cihaz Kodu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return true;
                }

                if (string.Equals(args[0], "--anahtar-olustur", StringComparison.OrdinalIgnoreCase))
                {
                    var keyArgMap = ParseArguments(args);
                    var publicKeyPath = GetRequiredArg(keyArgMap, "--public-cikis");
                    var privateKeyPath = GetRequiredArg(keyArgMap, "--ozel-cikis");
                    licenseService.GenerateKeyPair(publicKeyPath, privateKeyPath);

                    MessageBox.Show(
                        $"Anahtarlar oluşturuldu.\\nPublic: {publicKeyPath}\\nPrivate: {privateKeyPath}",
                        "Başarılı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return true;
                }

                if (!string.Equals(args[0], "--lisans-uret", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var licenseArgMap = ParseArguments(args);
                var request = new LicenseGenerationRequest
                {
                    CompanyName = GetRequiredArg(licenseArgMap, "--firma"),
                    LicenseType = GetRequiredArg(licenseArgMap, "--tip"),
                    StartDate = ParseDate(GetRequiredArg(licenseArgMap, "--baslangic")),
                    EndDate = ParseDate(GetRequiredArg(licenseArgMap, "--bitis")),
                    MachineCode = GetRequiredArg(licenseArgMap, "--makine"),
                    PrivateKeyPath = GetRequiredArg(licenseArgMap, "--ozel-anahtar"),
                    OutputPath = GetRequiredArg(licenseArgMap, "--cikis"),
                    ProductName = GetOptionalArg(licenseArgMap, "--urun") ?? "Rivada Restaurant POS",
                    LicenseVersion = GetOptionalArg(licenseArgMap, "--versiyon") ?? "1.0",
                    Notes = GetOptionalArg(licenseArgMap, "--not")
                };

                licenseService.GenerateLicenseFile(request);
                MessageBox.Show(
                    $"Lisans dosyası oluşturuldu:\n{request.OutputPath}",
                    "Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    $"Lisans üretimi başarısız: {ex.Message}",
                    "Lisans Üretim Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Lisans üretimi sırasında beklenmeyen bir hata oluştu.",
                    "Lisans Üretim Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return true;
            }
        }

        private static DateTime ParseDate(string dateText)
        {
            if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw new InvalidOperationException("Tarih formatı yyyy-MM-dd olmalıdır.");
            }

            return date;
        }

        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < args.Length; i++)
            {
                var key = args[i];
                if (!key.StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                if (i + 1 >= args.Length)
                {
                    break;
                }

                var value = args[i + 1];
                if (value.StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                map[key] = value;
                i++;
            }

            return map;
        }

        private static string GetRequiredArg(Dictionary<string, string> map, string key)
        {
            if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Parametre eksik: {key}");
            }

            return value.Trim();
        }

        private static string? GetOptionalArg(Dictionary<string, string> map, string key)
        {
            return map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        private static LoginForm CreateLoginForm()
        {
            var services = AppComposition.Create();
            return new LoginForm(
                services.AuthService,
                services.TableManagementService,
                services.OrderService,
                services.PaymentService,
                services.ProductManagementService);
        }

        private static void RegisterGlobalExceptionHandlers()
        {
            Application.ThreadException += (_, args) =>
            {
                ShowFatalError(args.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    ShowFatalError(ex);
                    return;
                }

                ShowFatalError(new Exception("Bilinmeyen kritik bir hata olustu."));
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                ShowFatalError(args.Exception);
                args.SetObserved();
            };
        }

        private static void ShowFatalError(Exception ex)
        {
            AppDiagnostics.ShowError(
                owner: null,
                userMessage: "Beklenmeyen bir uygulama hatasi olustu.",
                ex: ex,
                context: "Program.ShowFatalError",
                title: "Uygulama Hatasi");
        }
    }
}
