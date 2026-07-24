using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class EmployeeCertificate : AuditableEntity
    {
        public Guid EmployeeUserId { get; set; }
        public string CertificateName { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public string CredentialId { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpirationDate { get; set; }
        public bool IsMandatory { get; set; } = false;

        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
        public bool IsExpiringSoon => ExpirationDate.HasValue && ExpirationDate.Value >= DateTime.UtcNow && ExpirationDate.Value <= DateTime.UtcNow.AddDays(60);

        // Navigation
        public User? EmployeeUser { get; set; }
    }
}
