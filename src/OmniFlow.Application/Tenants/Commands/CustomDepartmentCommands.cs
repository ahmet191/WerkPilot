using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tenants.Commands
{
    public class CustomDepartmentDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DepartmentType { get; set; } = "Custom";
        public string IconName { get; set; } = "BankOutlined";
        public string ColorHex { get; set; } = "#8b5cf6";
        public Guid? ParentDepartmentId { get; set; }
        public string? ParentDepartmentName { get; set; }
        public bool IsActive { get; set; }
    }

    public record GetCustomDepartmentsQuery(Guid TenantId) : IRequest<List<CustomDepartmentDto>>;

    public class GetCustomDepartmentsQueryHandler : IRequestHandler<GetCustomDepartmentsQuery, List<CustomDepartmentDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetCustomDepartmentsQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<CustomDepartmentDto>> Handle(GetCustomDepartmentsQuery request, CancellationToken cancellationToken)
        {
            var depts = await _context.CustomDepartments
                .AsNoTracking()
                .Include(d => d.ParentDepartment)
                .Where(d => d.TenantId == request.TenantId)
                .OrderBy(d => d.Name)
                .Select(d => new CustomDepartmentDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    DepartmentType = d.DepartmentType,
                    IconName = d.IconName,
                    ColorHex = d.ColorHex,
                    ParentDepartmentId = d.ParentDepartmentId,
                    ParentDepartmentName = d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                    IsActive = d.IsActive
                })
                .ToListAsync(cancellationToken);

            // Default seed departments if empty
            if (!depts.Any())
            {
                return new List<CustomDepartmentDto>
                {
                    new() { Id = Guid.NewGuid(), Code = "EXEC", Name = "Executive & General Management", Description = "C-Level Leadership & Strategy", DepartmentType = "Executive", IconName = "CrownOutlined", ColorHex = "#8b5cf6", IsActive = true },
                    new() { Id = Guid.NewGuid(), Code = "ITSM", Name = "IT & Infrastructure Service Desk", Description = "IT Hardware, Software & System Access", DepartmentType = "IT", IconName = "LaptopOutlined", ColorHex = "#0ea5e9", IsActive = true },
                    new() { Id = Guid.NewGuid(), Code = "HRIS", Name = "Human Resources & Payroll", Description = "Talent, Onboarding & Personnel Files", DepartmentType = "HR", IconName = "TeamOutlined", ColorHex = "#ec4899", IsActive = true },
                    new() { Id = Guid.NewGuid(), Code = "FIN", Name = "Finance & Procurement Studio", Description = "Expenses, Invoicing & Purchasing", DepartmentType = "Finance", IconName = "DollarOutlined", ColorHex = "#10b981", IsActive = true },
                    new() { Id = Guid.NewGuid(), Code = "LEGAL", Name = "Legal & Contract Management", Description = "Contracts, GDPR & Compliance Services", DepartmentType = "Legal", IconName = "SafetyCertificateOutlined", ColorHex = "#f59e0b", IsActive = true },
                    new() { Id = Guid.NewGuid(), Code = "FAC", Name = "Facility & Operations Desk", Description = "Office Space, Maintenance & Security Passes", DepartmentType = "Facilities", IconName = "BankOutlined", ColorHex = "#6366f1", IsActive = true }
                };
            }

            return depts;
        }
    }

    public class CreateOrUpdateCustomDepartmentCommand : IRequest<Guid>
    {
        public Guid? Id { get; set; }
        public Guid TenantId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DepartmentType { get; set; } = "Custom";
        public string IconName { get; set; } = "BankOutlined";
        public string ColorHex { get; set; } = "#8b5cf6";
        public Guid? ParentDepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateOrUpdateCustomDepartmentCommandHandler : IRequestHandler<CreateOrUpdateCustomDepartmentCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;

        public CreateOrUpdateCustomDepartmentCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateOrUpdateCustomDepartmentCommand request, CancellationToken cancellationToken)
        {
            CustomDepartment dept;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                dept = await _context.CustomDepartments.FirstOrDefaultAsync(d => d.Id == request.Id.Value, cancellationToken)
                    ?? new CustomDepartment { TenantId = request.TenantId };
            }
            else
            {
                dept = new CustomDepartment { TenantId = request.TenantId };
                _context.CustomDepartments.Add(dept);
            }

            dept.Code = string.IsNullOrWhiteSpace(request.Code) ? request.Name.Substring(0, Math.Min(4, request.Name.Length)).ToUpperInvariant() : request.Code;
            dept.Name = request.Name;
            dept.Description = request.Description;
            dept.DepartmentType = request.DepartmentType;
            dept.IconName = request.IconName;
            dept.ColorHex = request.ColorHex;
            dept.ParentDepartmentId = request.ParentDepartmentId;
            dept.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);
            return dept.Id;
        }
    }
}
