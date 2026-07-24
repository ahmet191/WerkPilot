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

            return new SkillsAndCertificatesBundleDto
            {
                Skills = skills,
                Certificates = certs
            };
        }
    }
}
