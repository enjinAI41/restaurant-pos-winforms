using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.WinForms.Forms;
using Rivada.Restaurant.WinForms.Infrastructure;

namespace Rivada.Restaurant.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            RegisterGlobalExceptionHandlers();

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
