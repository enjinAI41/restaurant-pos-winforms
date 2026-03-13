using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rivada.Restaurant.WinForms.Forms;

namespace Rivada.Restaurant.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            RegisterGlobalExceptionHandlers();

            Application.Run(CreateLoginForm());
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
            MessageBox.Show(
                $"Beklenmeyen bir hata olustu.\n\n{ex.Message}",
                "Uygulama Hatasi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
