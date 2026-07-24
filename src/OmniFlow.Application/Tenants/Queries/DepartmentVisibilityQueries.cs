using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tenants.Queries
{
    public class ServiceDeskCardDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DepartmentType { get; set; } = "IT"; // "IT", "HR", "Finance", "Legal", "Facilities", "Custom"
        public bool IsConfidential { get; set; }
        public string IconName { get; set; } = "CustomerServiceOutlined";
        public string ColorHex { get; set; } = "#1890ff";
        public bool CanCreateTickets { get; set; } = true;
    }

    public class VisibilityMatrixItemDto
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string ServiceDeskKey { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public bool CanCreateTickets { get; set; }
        public bool IsConfidential { get; set; }
    }

    public record GetMyVisibleDesksQuery(Guid UserId, string RoleName) : IRequest<List<ServiceDeskCardDto>>;

    public class GetMyVisibleDesksQueryHandler : IRequestHandler<GetMyVisibleDesksQuery, List<ServiceDeskCardDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetMyVisibleDesksQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceDeskCardDto>> Handle(GetMyVisibleDesksQuery request, CancellationToken cancellationToken)
        {
            var desks = await _context.ServiceDesks
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);

            var visibilities = await _context.DepartmentVisibilities
                .AsNoTracking()
                .Where(v => v.RoleName == request.RoleName)
                .ToListAsync(cancellationToken);

            var result = new List<ServiceDeskCardDto>();

            // Default service desks if database table is currently sparse
            if (!desks.Any())
            {
                result.Add(new ServiceDeskCardDto
                {
                    Id = Guid.Parse("d2000000-0000-0000-0000-000000000000"),
                    Name = "IT Service Desk",
                    Description = "Hardware, software, networks, and IT access requests.",
                    DepartmentType = "IT",
                    IsConfidential = false,
                    IconName = "CustomerServiceOutlined",
                    ColorHex = "#1890ff",
                    CanCreateTickets = true
                });

                result.Add(new ServiceDeskCardDto
                {
                    Id = Guid.Parse("d2000000-0000-0000-0000-000000000001"),
                    Name = "HR Helpdesk & Özlük",
                    Description = "Payroll disputes, benefits, grievances, and HR inquiries (Confidential).",
                    DepartmentType = "HR",
                    IsConfidential = true,
                    IconName = "UserOutlined",
                    ColorHex = "#722ed1",
                    CanCreateTickets = true
                });

                result.Add(new ServiceDeskCardDto
                {
                    Id = Guid.Parse("d2000000-0000-0000-0000-000000000002"),
                    Name = "Finance & Expense Desk",
                    Description = "Expense claims, invoice approvals, and reimbursement requests.",
                    DepartmentType = "Finance",
                    IsConfidential = false,
                    IconName = "BankOutlined",
                    ColorHex = "#52c41a",
                    CanCreateTickets = true
                });

                result.Add(new ServiceDeskCardDto
                {
                    Id = Guid.Parse("d2000000-0000-0000-0000-000000000003"),
                    Name = "Legal & Compliance Desk",
                    Description = "Contract reviews, NDA requests, and legal compliance consultations.",
                    DepartmentType = "Legal",
                    IsConfidential = true,
                    IconName = "FileTextOutlined",
                    ColorHex = "#fa8c16",
                    CanCreateTickets = true
                });

                result.Add(new ServiceDeskCardDto
                {
                    Id = Guid.Parse("d2000000-0000-0000-0000-000000000004"),
                    Name = "Facilities & Office Management",
                    Description = "Building access, office maintenance, and workspace requests.",
                    DepartmentType = "Facilities",
                    IsConfidential = false,
                    IconName = "BuildOutlined",
                    ColorHex = "#eb2f96",
                    CanCreateTickets = true
                });

                return result;
            }

            foreach (var desk in desks)
            {
                var vis = visibilities.FirstOrDefault(v => v.ServiceDeskKey.Equals(desk.DepartmentType, StringComparison.OrdinalIgnoreCase));
                if (vis != null && !vis.IsVisible)
                {
                    continue; // Hidden by visibility matrix for this role
                }

                result.Add(new ServiceDeskCardDto
                {
                    Id = desk.Id,
                    Name = desk.Name,
                    Description = desk.Description,
                    DepartmentType = desk.DepartmentType,
                    IsConfidential = desk.IsConfidential,
                    IconName = string.IsNullOrWhiteSpace(desk.IconName) ? "CustomerServiceOutlined" : desk.IconName,
                    ColorHex = string.IsNullOrWhiteSpace(desk.ColorHex) ? "#1890ff" : desk.ColorHex,
                    CanCreateTickets = vis?.CanCreateTickets ?? true
                });
            }

            return result;
        }
    }

    public record GetVisibilityMatrixQuery : IRequest<List<VisibilityMatrixItemDto>>;

    public class GetVisibilityMatrixQueryHandler : IRequestHandler<GetVisibilityMatrixQuery, List<VisibilityMatrixItemDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetVisibilityMatrixQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<VisibilityMatrixItemDto>> Handle(GetVisibilityMatrixQuery request, CancellationToken cancellationToken)
        {
            return await _context.DepartmentVisibilities
                .AsNoTracking()
                .Select(v => new VisibilityMatrixItemDto
                {
                    Id = v.Id,
                    RoleName = v.RoleName,
                    ServiceDeskKey = v.ServiceDeskKey,
                    IsVisible = v.IsVisible,
                    CanCreateTickets = v.CanCreateTickets,
                    IsConfidential = v.IsConfidential
                })
                .ToListAsync(cancellationToken);
        }
    }

    public class UpdateVisibilityMatrixCommand : IRequest<bool>
    {
        public string RoleName { get; set; } = string.Empty;
        public string ServiceDeskKey { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public bool CanCreateTickets { get; set; }
        public bool IsConfidential { get; set; }
    }

    public class UpdateVisibilityMatrixCommandHandler : IRequestHandler<UpdateVisibilityMatrixCommand, bool>
    {
        private readonly IOmniFlowDbContext _context;

        public UpdateVisibilityMatrixCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateVisibilityMatrixCommand request, CancellationToken cancellationToken)
        {
            var item = await _context.DepartmentVisibilities
                .FirstOrDefaultAsync(v => v.RoleName == request.RoleName && v.ServiceDeskKey == request.ServiceDeskKey, cancellationToken);

            if (item == null)
            {
                item = new DepartmentVisibility
                {
                    RoleName = request.RoleName,
                    ServiceDeskKey = request.ServiceDeskKey
                };
                _context.DepartmentVisibilities.Add(item);
            }

            item.IsVisible = request.IsVisible;
            item.CanCreateTickets = request.CanCreateTickets;
            item.IsConfidential = request.IsConfidential;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
