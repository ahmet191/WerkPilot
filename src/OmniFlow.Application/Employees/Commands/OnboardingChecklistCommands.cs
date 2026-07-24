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

            // Default seeded onboarding tasks if table is currently empty for demo
            if (!tasks.Any())
            {
                var demoEmp = await _context.Users.FirstOrDefaultAsync(cancellationToken);
                var empId = demoEmp?.Id ?? Guid.NewGuid();
                var empName = demoEmp?.Name ?? "New Employee";

                return new List<OnboardingTaskDto>
                {
                    new() { Id = Guid.NewGuid(), EmployeeUserId = empId, EmployeeName = empName, Type = "Onboarding", TaskName = "Provision Google Workspace / M365 Email Account", Description = "Create corporate email and invite user.", AssignedDepartment = "IT", IsCompleted = true, CompletedAt = DateTime.UtcNow.AddDays(-2), CompletedByName = "Bob IT Agent", DueDate = DateTime.UtcNow.AddDays(-1) },
                    new() { Id = Guid.NewGuid(), EmployeeUserId = empId, EmployeeName = empName, Type = "Onboarding", TaskName = "Issue Corporate Laptop (MacBook Air/ThinkPad)", Description = "Prepare device image and hand over to employee.", AssignedDepartment = "IT", IsCompleted = false, DueDate = DateTime.UtcNow.AddDays(1) },
                    new() { Id = Guid.NewGuid(), EmployeeUserId = empId, EmployeeName = empName, Type = "Onboarding", TaskName = "Collect Signed Employment Contract & Özlük Documents", Description = "Collect ID photocopy, diploma, and signed agreement.", AssignedDepartment = "HR", IsCompleted = false, DueDate = DateTime.UtcNow.AddDays(2) },
                    new() { Id = Guid.NewGuid(), EmployeeUserId = empId, EmployeeName = empName, Type = "Onboarding", TaskName = "Issue Building Access Card & Meal Allowance Card", Description = "Encode RFID badge and register on turnstiles.", AssignedDepartment = "Facilities", IsCompleted = false, DueDate = DateTime.UtcNow.AddDays(1) }
                };
            }

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
