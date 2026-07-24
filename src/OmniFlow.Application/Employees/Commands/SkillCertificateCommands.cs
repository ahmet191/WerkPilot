using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Application.Employees.Commands
{
    public class AddEmployeeSkillCommand : IRequest<Guid>
    {
        public Guid EmployeeUserId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int ProficiencyLevel { get; set; }
    }

    public class AddEmployeeSkillCommandHandler : IRequestHandler<AddEmployeeSkillCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;

        public AddEmployeeSkillCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(AddEmployeeSkillCommand request, CancellationToken cancellationToken)
        {
            var skill = new EmployeeSkill
            {
                EmployeeUserId = request.EmployeeUserId,
                SkillName = request.SkillName,
                Category = request.Category,
                ProficiencyLevel = request.ProficiencyLevel,
                LastEvaluatedAt = DateTime.UtcNow
            };

            _context.EmployeeSkills.Add(skill);
            await _context.SaveChangesAsync(cancellationToken);
            return skill.Id;
        }
    }

    public class AddEmployeeCertificateCommand : IRequest<Guid>
    {
        public Guid EmployeeUserId { get; set; }
        public string CertificateName { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public string CredentialId { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class AddEmployeeCertificateCommandHandler : IRequestHandler<AddEmployeeCertificateCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;

        public AddEmployeeCertificateCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(AddEmployeeCertificateCommand request, CancellationToken cancellationToken)
        {
            var cert = new EmployeeCertificate
            {
                EmployeeUserId = request.EmployeeUserId,
                CertificateName = request.CertificateName,
                IssuingOrganization = request.IssuingOrganization,
                CredentialId = request.CredentialId,
                IssueDate = request.IssueDate,
                ExpirationDate = request.ExpirationDate,
                IsMandatory = request.IsMandatory
            };

            _context.EmployeeCertificates.Add(cert);
            await _context.SaveChangesAsync(cancellationToken);
            return cert.Id;
        }
    }
}
