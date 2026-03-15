using System;

namespace Rivada.Restaurant.WinForms.Licensing
{
    public class LicenseFileModel
    {
        public string CompanyName { get; set; } = string.Empty;
        public string LicenseType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string? Notes { get; set; }
        public string ProductName { get; set; } = "Rivada Restaurant POS";
        public string LicenseVersion { get; set; } = "1.0";
    }

    public enum LicenseValidationStatus
    {
        Valid,
        MissingFile,
        InvalidFormat,
        InvalidSignature,
        MachineMismatch,
        NotStarted,
        Expired,
        ClockRollback,
        Error
    }

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public LicenseValidationStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MachineCode { get; set; } = string.Empty;
        public LicenseFileModel? License { get; set; }
    }

    public class LicenseGenerationRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string LicenseType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string PrivateKeyPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string ProductName { get; set; } = "Rivada Restaurant POS";
        public string LicenseVersion { get; set; } = "1.0";
        public string? Notes { get; set; }
    }
}
