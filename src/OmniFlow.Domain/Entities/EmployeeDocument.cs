using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities
{
    public enum DocumentCategory
    {
        Contract = 1,
        IDCard = 2,
        TaxCertificate = 3,
        HealthReport = 4,
        SickLeaveAU = 5, // Krankmeldung / AU (DE)
        VisaPassport = 6,
        Diploma = 7,
        NDA = 8,
        Other = 99
    }

    public class EmployeeDocument : AuditableEntity
    {
        public Guid UserId { get; set; }
        public DocumentCategory Category { get; set; } = DocumentCategory.Other;
        public string Title { get; set; } = string.Empty;
        public string FileStorageKey { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
        public DateTime? ExpirationDate { get; set; }
        public bool IsConfidential { get; set; } = true;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }
}
