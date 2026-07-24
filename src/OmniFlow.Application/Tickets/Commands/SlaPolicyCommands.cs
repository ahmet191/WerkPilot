using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Tickets.Commands
{
    public class SlaPolicyRuleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public int ResponseTimeMinutes { get; set; }
        public int ResolutionTimeHours { get; set; }
        public int EscalateAfterPercent { get; set; }
        public Guid? EscalateToUserId { get; set; }
        public string? EscalateToUserName { get; set; }
        public bool NotifyManagerOnBreach { get; set; }
        public bool IsActive { get; set; }
    }

    public record GetSlaPolicyRulesQuery(Guid TenantId) : IRequest<List<SlaPolicyRuleDto>>;

    public class GetSlaPolicyRulesQueryHandler : IRequestHandler<GetSlaPolicyRulesQuery, List<SlaPolicyRuleDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetSlaPolicyRulesQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<SlaPolicyRuleDto>> Handle(GetSlaPolicyRulesQuery request, CancellationToken cancellationToken)
        {
            var rules = await _context.SlaPolicyRules
                .AsNoTracking()
                .Include(s => s.EscalateToUser)
                .Where(s => s.TenantId == request.TenantId)
                .OrderBy(s => s.Priority)
                .Select(s => new SlaPolicyRuleDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Priority = s.Priority.ToString(),
                    ResponseTimeMinutes = s.ResponseTimeMinutes,
                    ResolutionTimeHours = s.ResolutionTimeHours,
                    EscalateAfterPercent = s.EscalateAfterPercent,
                    EscalateToUserId = s.EscalateToUserId,
                    EscalateToUserName = s.EscalateToUser != null ? s.EscalateToUser.Name : null,
                    NotifyManagerOnBreach = s.NotifyManagerOnBreach,
                    IsActive = s.IsActive
                })
                .ToListAsync(cancellationToken);

            // Default seed SLA policies for demonstration if empty
            if (!rules.Any())
            {
                return new List<SlaPolicyRuleDto>
                {
                    new() { Id = Guid.NewGuid(), Name = "Critical P1 Outage SLA Policy", Priority = "Critical", ResponseTimeMinutes = 15, ResolutionTimeHours = 4, EscalateAfterPercent = 75, NotifyManagerOnBreach = true, IsActive = true },
                    new() { Id = Guid.NewGuid(), Name = "High Priority Business Interruption Policy", Priority = "High", ResponseTimeMinutes = 30, ResolutionTimeHours = 8, EscalateAfterPercent = 80, NotifyManagerOnBreach = true, IsActive = true },
                    new() { Id = Guid.NewGuid(), Name = "Standard Service Request Policy", Priority = "Medium", ResponseTimeMinutes = 120, ResolutionTimeHours = 24, EscalateAfterPercent = 85, NotifyManagerOnBreach = false, IsActive = true },
                    new() { Id = Guid.NewGuid(), Name = "Low Priority Inquiry Policy", Priority = "Low", ResponseTimeMinutes = 240, ResolutionTimeHours = 48, EscalateAfterPercent = 90, NotifyManagerOnBreach = false, IsActive = true }
                };
            }

            return rules;
        }
    }

    public class SaveSlaPolicyRuleCommand : IRequest<Guid>
    {
        public Guid? Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public int ResponseTimeMinutes { get; set; } = 60;
        public int ResolutionTimeHours { get; set; } = 24;
        public int EscalateAfterPercent { get; set; } = 80;
        public Guid? EscalateToUserId { get; set; }
        public bool NotifyManagerOnBreach { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class SaveSlaPolicyRuleCommandHandler : IRequestHandler<SaveSlaPolicyRuleCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;

        public SaveSlaPolicyRuleCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(SaveSlaPolicyRuleCommand request, CancellationToken cancellationToken)
        {
            Enum.TryParse<TicketPriority>(request.Priority, true, out var prio);

            SlaPolicyRule rule;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                rule = await _context.SlaPolicyRules.FirstOrDefaultAsync(s => s.Id == request.Id.Value, cancellationToken)
                    ?? new SlaPolicyRule { TenantId = request.TenantId };
            }
            else
            {
                rule = new SlaPolicyRule { TenantId = request.TenantId };
                _context.SlaPolicyRules.Add(rule);
            }

            rule.Name = request.Name;
            rule.Priority = prio;
            rule.ResponseTimeMinutes = request.ResponseTimeMinutes;
            rule.ResolutionTimeHours = request.ResolutionTimeHours;
            rule.EscalateAfterPercent = request.EscalateAfterPercent;
            rule.EscalateToUserId = request.EscalateToUserId;
            rule.NotifyManagerOnBreach = request.NotifyManagerOnBreach;
            rule.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);
            return rule.Id;
        }
    }
}
