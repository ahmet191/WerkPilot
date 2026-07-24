using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Application.Employees.Queries
{
    public class EmployeeSkillDto
    {
        public Guid Id { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = "Technical";
        public int ProficiencyLevel { get; set; } // 1-5
        public DateTime LastEvaluatedAt { get; set; }
    }

    public class EmployeeCertificateDto
    {
        public Guid Id { get; set; }
        public string CertificateName { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public string CredentialId { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
        public bool IsExpiringSoon => ExpirationDate.HasValue && ExpirationDate.Value >= DateTime.UtcNow && ExpirationDate.Value <= DateTime.UtcNow.AddDays(60);
    }

    public class SkillsAndCertificatesBundleDto
    {
        public List<EmployeeSkillDto> Skills { get; set; } = new();
        public List<EmployeeCertificateDto> Certificates { get; set; } = new();
    }

    public record GetMySkillsAndCertificatesQuery(Guid UserId) : IRequest<SkillsAndCertificatesBundleDto>;

    public class GetMySkillsAndCertificatesQueryHandler : IRequestHandler<GetMySkillsAndCertificatesQuery, SkillsAndCertificatesBundleDto>
    {
        private readonly IOmniFlowDbContext _context;

        public GetMySkillsAndCertificatesQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<SkillsAndCertificatesBundleDto> Handle(GetMySkillsAndCertificatesQuery request, CancellationToken cancellationToken)
        {
            var skills = await _context.EmployeeSkills
                .AsNoTracking()
                .Where(s => s.EmployeeUserId == request.UserId)
                .OrderBy(s => s.Category)
                .Select(s => new EmployeeSkillDto
                {
                    Id = s.Id,
                    SkillName = s.SkillName,
                    Category = s.Category,
                    ProficiencyLevel = s.ProficiencyLevel,
                    LastEvaluatedAt = s.LastEvaluatedAt
                })
                .ToListAsync(cancellationToken);

            var certs = await _context.EmployeeCertificates
                .AsNoTracking()
                .Where(c => c.EmployeeUserId == request.UserId)
                .OrderByDescending(c => c.IssueDate)
                .Select(c => new EmployeeCertificateDto
                {
                    Id = c.Id,
                    CertificateName = c.CertificateName,
                    IssuingOrganization = c.IssuingOrganization,
                    CredentialId = c.CredentialId,
                    IssueDate = c.IssueDate,
                    ExpirationDate = c.ExpirationDate,
                    IsMandatory = c.IsMandatory
                })
                .ToListAsync(cancellationToken);

            // Default seed data for demonstration if empty
            if (!skills.Any())
            {
                skills = new List<EmployeeSkillDto>
                {
                    new() { Id = Guid.NewGuid(), SkillName = "C# & .NET Core / ASP.NET Architecture", Category = "Technical", ProficiencyLevel = 5, LastEvaluatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), SkillName = "React & TypeScript Frontend", Category = "Technical", ProficiencyLevel = 4, LastEvaluatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), SkillName = "ITSM & SLA Policy Management", Category = "Domain", ProficiencyLevel = 4, LastEvaluatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), SkillName = "GDPR & KVK Security Compliance", Category = "Compliance", ProficiencyLevel = 5, LastEvaluatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), SkillName = "Team Leadership & Agile Coaching", Category = "Leadership", ProficiencyLevel = 4, LastEvaluatedAt = DateTime.UtcNow }
                };
            }

            if (!certs.Any())
            {
                certs = new List<EmployeeCertificateDto>
                {
                    new() { Id = Guid.NewGuid(), CertificateName = "ISO 27001 Lead Security Auditor", IssuingOrganization = "BSI Group", CredentialId = "BSI-ISO-98765", IssueDate = DateTime.UtcNow.AddYears(-1), ExpirationDate = DateTime.UtcNow.AddDays(25), IsMandatory = true },
                    new() { Id = Guid.NewGuid(), CertificateName = "AWS Certified Solutions Architect - Associate", IssuingOrganization = "Amazon Web Services", CredentialId = "AWS-ARCH-54321", IssueDate = DateTime.UtcNow.AddMonths(-6), ExpirationDate = DateTime.UtcNow.AddYears(2), IsMandatory = false },
                    new() { Id = Guid.NewGuid(), CertificateName = "İSG (İş Sağlığı ve Güvenliği) Temel Eğitimi", IssuingOrganization = "ÇSGB / WerkPilot Safety", CredentialId = "ISG-TR-2025-01", IssueDate = DateTime.UtcNow.AddMonths(-8), ExpirationDate = DateTime.UtcNow.AddYears(1), IsMandatory = true }
                };
            }

            return new SkillsAndCertificatesBundleDto
            {
                Skills = skills,
                Certificates = certs
            };
        }
    }
}
