using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Rivada.Restaurant.WinForms.Licensing
{
    public class OfflineLicenseService
    {
        private const string PublicKeyXml = "<RSAKeyValue><Modulus>r3cvm/JZT53BI6jR5sYbsKnYh0GxF6aex+yjxKCPsMyow4FfJxfkBN0aH+OcNe+9N+wV9BwOr5CvvfGsaYcL9HglkdpMvc+IQXZj3Y10CuJRnR+OBFWb7k2EwqW+6xpm4BdFG0+8ru0pd5EQjkJn2ShG+vF8mybr51P1zIs+qwFZKw2I/1xjOqwAqG5DI2/unXvn7otdGcEqhkeWeTKkQdZuXONjS6IzYeu6W6tFVklSWUqu/Ppfx0xn7G3kRpU00jfUA8EHt6S37Lf4NIvguqxSrgocOm1KaGnqtIg9cEGz8R02ClbtqLm2WvJTGkPvLcVv8aRbLnE2SsRanJae1Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private const string StateSignatureSalt = "RivadaRestaurantLocalStateSalt-v1";

        private readonly string _licenseFilePath;
        private readonly string _stateFilePath;
        private readonly string _publicKeyFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public OfflineLicenseService(string licenseFilePath, string stateFilePath)
        {
            _licenseFilePath = licenseFilePath;
            _stateFilePath = stateFilePath;
            _publicKeyFilePath = Path.Combine(
                Path.GetDirectoryName(_licenseFilePath) ?? AppContext.BaseDirectory,
                "license-public-key.xml");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public string GetMachineCode()
        {
            return MachineCodeProvider.GetMachineCode();
        }

        public string GetLicenseFilePath()
        {
            return _licenseFilePath;
        }

        public LicenseValidationResult ValidateCurrentLicense()
        {
            var machineCode = GetMachineCode();

            try
            {
                if (!File.Exists(_licenseFilePath))
                {
                    return Invalid(LicenseValidationStatus.MissingFile, machineCode, "Lisans dosyası bulunamadı. Lütfen yeni lisans yükleyin.");
                }

                var content = File.ReadAllText(_licenseFilePath, Encoding.UTF8);
                var license = JsonSerializer.Deserialize<LicenseFileModel>(content, _jsonOptions);
                if (!IsLicenseModelValid(license))
                {
                    return Invalid(LicenseValidationStatus.InvalidFormat, machineCode, "Lisans doğrulanamadı. Lütfen yeni lisans yükleyin.", license);
                }

                if (!VerifySignature(license!))
                {
                    return Invalid(LicenseValidationStatus.InvalidSignature, machineCode, "Lisans doğrulanamadı. Lütfen yeni lisans yükleyin.", license);
                }

                if (!string.Equals(license!.MachineCode, machineCode, StringComparison.OrdinalIgnoreCase))
                {
                    return Invalid(LicenseValidationStatus.MachineMismatch, machineCode, "Bu lisans bu bilgisayar için geçerli değildir.", license);
                }

                var today = DateTime.Today;
                var startDate = license.StartDate.Date;
                var endDate = license.EndDate.Date;

                if (today < startDate)
                {
                    return Invalid(LicenseValidationStatus.NotStarted, machineCode, "Lisans henüz başlamamıştır.", license);
                }

                if (today > endDate)
                {
                    return Invalid(LicenseValidationStatus.Expired, machineCode, "Lisans süresi dolmuştur. Lütfen yeni lisans yükleyin.", license);
                }

                if (IsClockRollbackDetected(machineCode, today))
                {
                    return Invalid(LicenseValidationStatus.ClockRollback, machineCode, "Sistem tarihi geriye alınmış görünüyor. Lütfen tarih ayarlarını kontrol edin veya yeni lisans yükleyin.", license);
                }

                SaveLastSuccessfulRun(machineCode, today);
                return new LicenseValidationResult
                {
                    IsValid = true,
                    Status = LicenseValidationStatus.Valid,
                    Message = "Lisans geçerli.",
                    MachineCode = machineCode,
                    License = license
                };
            }
            catch
            {
                return Invalid(LicenseValidationStatus.Error, machineCode, "Lisans doğrulanamadı. Lütfen yeni lisans yükleyin.");
            }
        }

        public LicenseValidationResult ImportLicense(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return Invalid(LicenseValidationStatus.MissingFile, GetMachineCode(), "Lisans dosyası bulunamadı.");
            }

            try
            {
                var targetDirectory = Path.GetDirectoryName(_licenseFilePath);
                if (!string.IsNullOrWhiteSpace(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourceFilePath, _licenseFilePath, true);
                return ValidateCurrentLicense();
            }
            catch
            {
                return Invalid(LicenseValidationStatus.Error, GetMachineCode(), "Lisans dosyası yüklenemedi.");
            }
        }

        public void GenerateLicenseFile(LicenseGenerationRequest request)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Lisans üretim parametreleri eksik.");
            }

            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                throw new InvalidOperationException("Firma adı boş bırakılamaz.");
            }

            if (string.IsNullOrWhiteSpace(request.LicenseType))
            {
                throw new InvalidOperationException("Lisans tipi boş bırakılamaz.");
            }

            if (string.IsNullOrWhiteSpace(request.MachineCode))
            {
                throw new InvalidOperationException("Makine kodu boş bırakılamaz.");
            }

            if (request.EndDate.Date < request.StartDate.Date)
            {
                throw new InvalidOperationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
            }

            if (string.IsNullOrWhiteSpace(request.PrivateKeyPath) || !File.Exists(request.PrivateKeyPath))
            {
                throw new InvalidOperationException("Özel anahtar dosyası bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(request.OutputPath))
            {
                throw new InvalidOperationException("Çıkış dosyası yolu boş bırakılamaz.");
            }

            var privateKeyXml = File.ReadAllText(request.PrivateKeyPath, Encoding.UTF8);
            var license = new LicenseFileModel
            {
                CompanyName = request.CompanyName.Trim(),
                LicenseType = request.LicenseType.Trim(),
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                MachineCode = request.MachineCode.Trim(),
                IssuedAt = DateTime.UtcNow,
                ProductName = string.IsNullOrWhiteSpace(request.ProductName) ? "Rivada Restaurant POS" : request.ProductName.Trim(),
                LicenseVersion = string.IsNullOrWhiteSpace(request.LicenseVersion) ? "1.0" : request.LicenseVersion.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            license.Signature = SignLicense(license, privateKeyXml);

            var outputDirectory = Path.GetDirectoryName(request.OutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var json = JsonSerializer.Serialize(license, _jsonOptions);
            File.WriteAllText(request.OutputPath, json, Encoding.UTF8);
        }

        public void GenerateKeyPair(string publicKeyOutputPath, string privateKeyOutputPath)
        {
            if (string.IsNullOrWhiteSpace(publicKeyOutputPath) || string.IsNullOrWhiteSpace(privateKeyOutputPath))
            {
                throw new InvalidOperationException("Anahtar çıkış yolları boş bırakılamaz.");
            }

            using var rsa = RSA.Create(2048);
            var privateParameters = rsa.ExportParameters(true);
            var publicParameters = rsa.ExportParameters(false);

            var privateKeyXml = ToXmlString(privateParameters, includePrivateParameters: true);
            var publicKeyXml = ToXmlString(publicParameters, includePrivateParameters: false);

            var privateDir = Path.GetDirectoryName(privateKeyOutputPath);
            if (!string.IsNullOrWhiteSpace(privateDir) && !Directory.Exists(privateDir))
            {
                Directory.CreateDirectory(privateDir);
            }

            var publicDir = Path.GetDirectoryName(publicKeyOutputPath);
            if (!string.IsNullOrWhiteSpace(publicDir) && !Directory.Exists(publicDir))
            {
                Directory.CreateDirectory(publicDir);
            }

            File.WriteAllText(privateKeyOutputPath, privateKeyXml, Encoding.UTF8);
            File.WriteAllText(publicKeyOutputPath, publicKeyXml, Encoding.UTF8);
        }

        private static bool IsLicenseModelValid(LicenseFileModel? license)
        {
            return license != null &&
                   !string.IsNullOrWhiteSpace(license.CompanyName) &&
                   !string.IsNullOrWhiteSpace(license.LicenseType) &&
                   !string.IsNullOrWhiteSpace(license.MachineCode) &&
                   !string.IsNullOrWhiteSpace(license.Signature);
        }

        private static LicenseValidationResult Invalid(
            LicenseValidationStatus status,
            string machineCode,
            string message,
            LicenseFileModel? license = null)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = status,
                Message = message,
                MachineCode = machineCode,
                License = license
            };
        }

        private static string BuildPayload(LicenseFileModel license)
        {
            return string.Join("|", new[]
            {
                Normalize(license.CompanyName),
                Normalize(license.LicenseType),
                license.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                license.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Normalize(license.MachineCode),
                license.IssuedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                Normalize(license.ProductName),
                Normalize(license.LicenseVersion),
                Normalize(license.Notes)
            });
        }

        private static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim().Replace("|", "/");
        }

        private static string SignLicense(LicenseFileModel license, string privateKeyXml)
        {
            var payloadBytes = Encoding.UTF8.GetBytes(BuildPayload(license));
            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);
            var signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureBytes);
        }

        private bool VerifySignature(LicenseFileModel license)
        {
            if (string.IsNullOrWhiteSpace(license.Signature))
            {
                return false;
            }

            try
            {
                var payloadBytes = Encoding.UTF8.GetBytes(BuildPayload(license));
                var signatureBytes = Convert.FromBase64String(license.Signature);
                using var rsa = RSA.Create();
                rsa.FromXmlString(GetPublicKeyXml());
                return rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        private string GetPublicKeyXml()
        {
            try
            {
                if (File.Exists(_publicKeyFilePath))
                {
                    var fileContent = File.ReadAllText(_publicKeyFilePath, Encoding.UTF8).Trim();
                    if (!string.IsNullOrWhiteSpace(fileContent))
                    {
                        return fileContent;
                    }
                }
            }
            catch
            {
                // Dosya okunamazsa gömülü public key kullanılır.
            }

            return PublicKeyXml;
        }

        private bool IsClockRollbackDetected(string machineCode, DateTime currentDate)
        {
            var state = LoadState();
            if (state == null)
            {
                return false;
            }

            if (!IsStateSignatureValid(state, machineCode))
            {
                return false;
            }

            if (!DateTime.TryParseExact(state.LastSuccessfulRunDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastRunDate))
            {
                return false;
            }

            return currentDate.Date < lastRunDate.Date.AddDays(-1);
        }

        private void SaveLastSuccessfulRun(string machineCode, DateTime currentDate)
        {
            var existing = LoadState();
            var finalDate = currentDate.Date;

            if (existing != null &&
                IsStateSignatureValid(existing, machineCode) &&
                DateTime.TryParseExact(existing.LastSuccessfulRunDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastRunDate) &&
                lastRunDate.Date > finalDate)
            {
                finalDate = lastRunDate.Date;
            }

            var state = new LicenseStateModel
            {
                LastSuccessfulRunDate = finalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
            state.Signature = BuildStateSignature(state.LastSuccessfulRunDate, machineCode);

            var json = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(_stateFilePath, json, Encoding.UTF8);
        }

        private LicenseStateModel? LoadState()
        {
            try
            {
                if (!File.Exists(_stateFilePath))
                {
                    return null;
                }

                var content = File.ReadAllText(_stateFilePath, Encoding.UTF8);
                return JsonSerializer.Deserialize<LicenseStateModel>(content, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsStateSignatureValid(LicenseStateModel state, string machineCode)
        {
            if (string.IsNullOrWhiteSpace(state.LastSuccessfulRunDate) || string.IsNullOrWhiteSpace(state.Signature))
            {
                return false;
            }

            var expected = BuildStateSignature(state.LastSuccessfulRunDate, machineCode);
            return string.Equals(expected, state.Signature, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildStateSignature(string dateText, string machineCode)
        {
            using var sha = SHA256.Create();
            var payload = $"{dateText}|{machineCode}|{StateSignatureSalt}";
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }

        private static string ToXmlString(RSAParameters parameters, bool includePrivateParameters)
        {
            var sb = new StringBuilder();
            sb.Append("<RSAKeyValue>");
            sb.Append("<Modulus>").Append(Convert.ToBase64String(parameters.Modulus ?? Array.Empty<byte>())).Append("</Modulus>");
            sb.Append("<Exponent>").Append(Convert.ToBase64String(parameters.Exponent ?? Array.Empty<byte>())).Append("</Exponent>");

            if (includePrivateParameters)
            {
                sb.Append("<P>").Append(Convert.ToBase64String(parameters.P ?? Array.Empty<byte>())).Append("</P>");
                sb.Append("<Q>").Append(Convert.ToBase64String(parameters.Q ?? Array.Empty<byte>())).Append("</Q>");
                sb.Append("<DP>").Append(Convert.ToBase64String(parameters.DP ?? Array.Empty<byte>())).Append("</DP>");
                sb.Append("<DQ>").Append(Convert.ToBase64String(parameters.DQ ?? Array.Empty<byte>())).Append("</DQ>");
                sb.Append("<InverseQ>").Append(Convert.ToBase64String(parameters.InverseQ ?? Array.Empty<byte>())).Append("</InverseQ>");
                sb.Append("<D>").Append(Convert.ToBase64String(parameters.D ?? Array.Empty<byte>())).Append("</D>");
            }

            sb.Append("</RSAKeyValue>");
            return sb.ToString();
        }

        private class LicenseStateModel
        {
            public string LastSuccessfulRunDate { get; set; } = string.Empty;
            public string Signature { get; set; } = string.Empty;
        }
    }

    internal static class RsaXmlExtensions
    {
        public static void FromXmlString(this RSA rsa, string xmlString)
        {
            var parameters = new RSAParameters();
            var xml = new System.Xml.XmlDocument();
            xml.LoadXml(xmlString);

            if (xml.DocumentElement?.Name != "RSAKeyValue")
            {
                throw new InvalidOperationException("Geçersiz RSA anahtar formatı.");
            }

            foreach (System.Xml.XmlNode node in xml.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                    case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                    case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                    case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                    case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                    case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                    case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                    case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                }
            }

            rsa.ImportParameters(parameters);
        }
    }
}
