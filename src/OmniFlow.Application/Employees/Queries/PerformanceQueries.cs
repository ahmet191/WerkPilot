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
    public class EmployeeGoalDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Quarter { get; set; } = "Q1";
        public int Year { get; set; }
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public string Unit { get; set; } = "Percentage";
        public decimal ProgressPercent { get; set; }
        public string Status { get; set; } = "InProgress";
    }

    public record GetMyGoalsQuery(Guid UserId, int? Year) : IRequest<List<EmployeeGoalDto>>;

    public class GetMyGoalsQueryHandler : IRequestHandler<GetMyGoalsQuery, List<EmployeeGoalDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetMyGoalsQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeGoalDto>> Handle(GetMyGoalsQuery request, CancellationToken cancellationToken)
        {
            var targetYear = request.Year ?? DateTime.UtcNow.Year;

            var goals = await _context.EmployeeGoals
                .AsNoTracking()
                .Where(g => g.EmployeeUserId == request.UserId && g.Year == targetYear)
                .OrderBy(g => g.Quarter)
                .Select(g => new EmployeeGoalDto
                {
                    Id = g.Id,
                    EmployeeUserId = g.EmployeeUserId,
                    Title = g.Title,
                    Description = g.Description,
                    Quarter = g.Quarter.ToString(),
                    Year = g.Year,
                    TargetValue = g.TargetValue,
                    CurrentValue = g.CurrentValue,
                    Unit = g.Unit,
                    ProgressPercent = g.ProgressPercent,
                    Status = g.Status.ToString()
                })
                .ToListAsync(cancellationToken);

            return goals;
        }
    }

    public class CreateOrUpdateGoalCommand : IRequest<Guid>
    {
        public Guid? Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Quarter { get; set; } = "Q1";
        public int Year { get; set; } = 2026;
        public decimal TargetValue { get; set; } = 100m;
        public decimal CurrentValue { get; set; } = 0m;
        public string Unit { get; set; } = "Percentage";
        public string Status { get; set; } = "InProgress";
    }

    public class CreateOrUpdateGoalCommandHandler : IRequestHandler<CreateOrUpdateGoalCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;

        public CreateOrUpdateGoalCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateOrUpdateGoalCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User '{request.UserId}' not found.");
            }

            Enum.TryParse<GoalQuarter>(request.Quarter, true, out var qtr);
            Enum.TryParse<GoalStatus>(request.Status, true, out var st);

            EmployeeGoal goal;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                goal = await _context.EmployeeGoals.FirstOrDefaultAsync(g => g.Id == request.Id.Value, cancellationToken)
                    ?? new EmployeeGoal { TenantId = user.TenantId, EmployeeUserId = request.UserId };
            }
            else
            {
                goal = new EmployeeGoal { TenantId = user.TenantId, EmployeeUserId = request.UserId };
                _context.EmployeeGoals.Add(goal);
            }

            goal.Title = request.Title;
            goal.Description = request.Description;
            goal.Quarter = qtr;
            goal.Year = request.Year;
            goal.TargetValue = request.TargetValue;
            goal.CurrentValue = request.CurrentValue;
            goal.Unit = request.Unit;
            goal.Status = st;

            await _context.SaveChangesAsync(cancellationToken);
            return goal.Id;
        }
    }

    public record DeleteGoalCommand(Guid GoalId, Guid UserId) : IRequest<bool>;

    public class DeleteGoalCommandHandler : IRequestHandler<DeleteGoalCommand, bool>
    {
        private readonly IOmniFlowDbContext _context;

        public DeleteGoalCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _context.EmployeeGoals.FirstOrDefaultAsync(g => g.Id == request.GoalId && g.EmployeeUserId == request.UserId, cancellationToken);
            if (goal == null)
            {
                throw new KeyNotFoundException($"Goal '{request.GoalId}' not found or doesn't belong to the user.");
            }

            _context.EmployeeGoals.Remove(goal);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
