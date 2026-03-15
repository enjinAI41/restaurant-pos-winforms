using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Rivada.Restaurant.WinForms.Licensing
{
    public static class MachineCodeProvider
    {
        public static string GetMachineCode()
        {
            var source = BuildStableSource();
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
            var hex = Convert.ToHexString(hash);
            return string.Join("-", new[]
            {
                hex.Substring(0, 8),
                hex.Substring(8, 8),
                hex.Substring(16, 8),
                hex.Substring(24, 8)
            });
        }

        private static string BuildStableSource()
        {
            var machineGuid = ReadMachineGuid();
            var machineName = Environment.MachineName;
            var osVersion = Environment.OSVersion.VersionString;
            return $"{machineGuid}|{machineName}|{osVersion}";
        }

        private static string ReadMachineGuid()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", false);
                var value = key?.GetValue("MachineGuid")?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
            catch
            {
                // Makine bilgisi okunamıyorsa fallback kullanılır.
            }

            return Environment.MachineName;
        }
    }
}
