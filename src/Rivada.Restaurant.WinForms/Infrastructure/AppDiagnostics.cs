using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Rivada.Restaurant.WinForms.Infrastructure
{
    internal static class AppDiagnostics
    {
        private static readonly object SyncLock = new object();
        private static readonly string LogDirectoryPath = Path.Combine(AppContext.BaseDirectory, "logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectoryPath, "app-errors.log");

        public static void ShowError(
            IWin32Window? owner,
            string userMessage,
            Exception ex,
            string context,
            string title = "Hata")
        {
            var errorCode = LogException(ex, context);
            var message = $"{userMessage}\n\nHata Kodu: {errorCode}";
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static string LogException(Exception ex, string context)
        {
            var errorCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sb = new StringBuilder();
            sb.AppendLine($"[{now}] Kod: {errorCode}");
            sb.AppendLine($"Baglam: {context}");
            sb.AppendLine($"Tur: {ex.GetType().FullName}");
            sb.AppendLine($"Mesaj: {ex.Message}");
            sb.AppendLine("Ayrinti:");
            sb.AppendLine(ex.ToString());
            sb.AppendLine(new string('-', 80));

            Debug.WriteLine(sb.ToString());

            try
            {
                lock (SyncLock)
                {
                    if (!Directory.Exists(LogDirectoryPath))
                    {
                        Directory.CreateDirectory(LogDirectoryPath);
                    }

                    File.AppendAllText(LogFilePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Log yazma hatasi ana akisi bozmasin.
            }

            return errorCode;
        }
    }
}
