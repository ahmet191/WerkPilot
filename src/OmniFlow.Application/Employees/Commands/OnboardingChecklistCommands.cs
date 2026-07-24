using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Application.Employees.Commands
{
    public class OnboardingTaskDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeUserId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Type { get; set; } = "Onboarding";
        public string TaskName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssignedDepartment { get; set; } = "IT";
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CompletedByName { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public record GetOnboardingChecklistQuery(Guid? EmployeeUserId, string? DepartmentFilter) : IRequest<List<OnboardingTaskDto>>;

    public class GetOnboardingChecklistQueryHandler : IRequestHandler<GetOnboardingChecklistQuery, List<OnboardingTaskDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetOnboardingChecklistQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<OnboardingTaskDto>> Handle(GetOnboardingChecklistQuery request, CancellationToken cancellationToken)
        {
            var query = _context.OnboardingChecklists
                .AsNoTracking()
                .Include(c => c.EmployeeUser)
                .Include(c => c.CompletedByUser)
                .AsQueryable();

            if (request.EmployeeUserId.HasValue && request.EmployeeUserId.Value != Guid.Empty)
            {
                query = query.Where(c => c.EmployeeUserId == request.EmployeeUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.DepartmentFilter))
            {
                if (Enum.TryParse<ChecklistDepartment>(request.DepartmentFilter, true, out var dept))
                {
                    query = query.Where(c => c.AssignedDepartment == dept);
                }
            }

            var tasks = await query.OrderBy(c => c.IsCompleted).ThenBy(c => c.DueDate).ToListAsync(cancellationToken);

            return tasks.Select(c => new OnboardingTaskDto
            {
                Id = c.Id,
                EmployeeUserId = c.EmployeeUserId,
                EmployeeName = c.EmployeeUser?.Name ?? "Employee",
                Type = c.Type.ToString(),
                TaskName = c.TaskName,
                Description = c.Description,
                AssignedDepartment = c.AssignedDepartment.ToString(),
                IsCompleted = c.IsCompleted,
                CompletedAt = c.CompletedAt,
                CompletedByName = c.CompletedByUser?.Name,
                DueDate = c.DueDate
            }).ToList();
        }
    }

    public class CompleteOnboardingTaskCommand : IRequest<bool>
    {
        public Guid TaskId { get; set; }
        public Guid CompletedByUserId { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class CompleteOnboardingTaskCommandHandler : IRequestHandler<CompleteOnboardingTaskCommand, bool>
    {
        private readonly IOmniFlowDbContext _context;

        public CompleteOnboardingTaskCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(CompleteOnboardingTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.OnboardingChecklists.FirstOrDefaultAsync(c => c.Id == request.TaskId, cancellationToken);
            if (task == null)
            {
                return false;
            }

            task.IsCompleted = request.IsCompleted;
            task.CompletedAt = request.IsCompleted ? DateTime.UtcNow : null;
            task.CompletedByUserId = request.IsCompleted ? request.CompletedByUserId : null;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
