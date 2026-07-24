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

    // ── Skills: create/update + delete (owner-scoped) ───────────────────

    public class SaveEmployeeSkillCommand : IRequest<Guid>
    {
        public Guid? Id { get; set; }
        public Guid UserId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = "Technical";
        public int ProficiencyLevel { get; set; } = 3;
    }

    public class SaveEmployeeSkillCommandHandler : IRequestHandler<SaveEmployeeSkillCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;
        public SaveEmployeeSkillCommandHandler(IOmniFlowDbContext context) => _context = context;

        public async Task<Guid> Handle(SaveEmployeeSkillCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{request.UserId}' not found.");

            EmployeeSkill skill;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                skill = await _context.EmployeeSkills.FirstOrDefaultAsync(s => s.Id == request.Id.Value && s.EmployeeUserId == request.UserId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Skill '{request.Id}' not found or doesn't belong to the user.");
            }
            else
            {
                skill = new EmployeeSkill { TenantId = user.TenantId, EmployeeUserId = request.UserId };
                _context.EmployeeSkills.Add(skill);
            }

            skill.SkillName = request.SkillName;
            skill.Category = request.Category;
            skill.ProficiencyLevel = Math.Clamp(request.ProficiencyLevel, 1, 5);
            skill.LastEvaluatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return skill.Id;
        }
    }

    public record DeleteEmployeeSkillCommand(Guid Id, Guid UserId) : IRequest<bool>;

    public class DeleteEmployeeSkillCommandHandler : IRequestHandler<DeleteEmployeeSkillCommand, bool>
    {
        private readonly IOmniFlowDbContext _context;
        public DeleteEmployeeSkillCommandHandler(IOmniFlowDbContext context) => _context = context;

        public async Task<bool> Handle(DeleteEmployeeSkillCommand request, CancellationToken cancellationToken)
        {
            var skill = await _context.EmployeeSkills.FirstOrDefaultAsync(s => s.Id == request.Id && s.EmployeeUserId == request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException($"Skill '{request.Id}' not found or doesn't belong to the user.");
            _context.EmployeeSkills.Remove(skill);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    // ── Certificates: create/update + delete (owner-scoped) ─────────────

    public class SaveEmployeeCertificateCommand : IRequest<Guid>
    {
        public Guid? Id { get; set; }
        public Guid UserId { get; set; }
        public string CertificateName { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public string CredentialId { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpirationDate { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class SaveEmployeeCertificateCommandHandler : IRequestHandler<SaveEmployeeCertificateCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;
        public SaveEmployeeCertificateCommandHandler(IOmniFlowDbContext context) => _context = context;

        public async Task<Guid> Handle(SaveEmployeeCertificateCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{request.UserId}' not found.");

            EmployeeCertificate cert;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                cert = await _context.EmployeeCertificates.FirstOrDefaultAsync(c => c.Id == request.Id.Value && c.EmployeeUserId == request.UserId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Certificate '{request.Id}' not found or doesn't belong to the user.");
            }
            else
            {
                cert = new EmployeeCertificate { TenantId = user.TenantId, EmployeeUserId = request.UserId };
                _context.EmployeeCertificates.Add(cert);
            }

            cert.CertificateName = request.CertificateName;
            cert.IssuingOrganization = request.IssuingOrganization;
            cert.CredentialId = request.CredentialId;
            cert.IssueDate = DateTime.SpecifyKind(request.IssueDate, DateTimeKind.Utc);
            cert.ExpirationDate = request.ExpirationDate.HasValue
                ? DateTime.SpecifyKind(request.ExpirationDate.Value, DateTimeKind.Utc)
                : null;
            cert.IsMandatory = request.IsMandatory;

            await _context.SaveChangesAsync(cancellationToken);
            return cert.Id;
        }
    }

    public record DeleteEmployeeCertificateCommand(Guid Id, Guid UserId) : IRequest<bool>;

    public class DeleteEmployeeCertificateCommandHandler : IRequestHandler<DeleteEmployeeCertificateCommand, bool>
    {
        private readonly IOmniFlowDbContext _context;
        public DeleteEmployeeCertificateCommandHandler(IOmniFlowDbContext context) => _context = context;

        public async Task<bool> Handle(DeleteEmployeeCertificateCommand request, CancellationToken cancellationToken)
        {
            var cert = await _context.EmployeeCertificates.FirstOrDefaultAsync(c => c.Id == request.Id && c.EmployeeUserId == request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException($"Certificate '{request.Id}' not found or doesn't belong to the user.");
            _context.EmployeeCertificates.Remove(cert);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
