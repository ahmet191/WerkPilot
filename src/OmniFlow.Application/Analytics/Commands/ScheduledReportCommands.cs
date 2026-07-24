using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Analytics.Commands
{
    public class ScheduledReportDto
    {
        public Guid Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public string Recipients { get; set; } = string.Empty;
        public string Format { get; set; } = "PDF";
        public DateTime? NextRunTime { get; set; }
        public bool IsActive { get; set; }
    }

    public record GetScheduledReportsQuery(Guid TenantId) : IRequest<List<ScheduledReportDto>>;

    public class GetScheduledReportsQueryHandler : IRequestHandler<GetScheduledReportsQuery, List<ScheduledReportDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetScheduledReportsQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<ScheduledReportDto>> Handle(GetScheduledReportsQuery request, CancellationToken cancellationToken)
        {
            var reports = await _context.ScheduledReports
                .AsNoTracking()
                .Where(r => r.TenantId == request.TenantId)
                .Select(r => new ScheduledReportDto
                {
                    Id = r.Id,
                    ReportName = r.ReportName,
                    CronExpression = r.CronExpression,
                    Recipients = r.Recipients,
                    Format = r.Format,
                    NextRunTime = r.NextRunTime,
                    IsActive = r.IsActive
                })
                .ToListAsync(cancellationToken);

            if (!reports.Any())
            {
                // Return some mock initial state
                return new List<ScheduledReportDto>
                {
                    new() { Id = Guid.NewGuid(), ReportName = "Weekly IT Service Desk Performance", CronExpression = "0 9 * * 1", Recipients = "it-management@example.com", Format = "PDF", IsActive = true, NextRunTime = DateTime.UtcNow.AddDays(3) },
                    new() { Id = Guid.NewGuid(), ReportName = "Monthly SLA Breach Report", CronExpression = "0 8 1 * *", Recipients = "compliance@example.com", Format = "Excel", IsActive = true, NextRunTime = DateTime.UtcNow.AddDays(15) }
                };
            }

            return reports;
        }
    }

    public class CreateScheduledReportCommand : IRequest<Guid>
    {
        public Guid TenantId { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public string Recipients { get; set; } = string.Empty;
        public string Format { get; set; } = "PDF";
        public bool IsActive { get; set; } = true;
    }

    public class CreateScheduledReportCommandHandler : IRequestHandler<CreateScheduledReportCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;

        public CreateScheduledReportCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateScheduledReportCommand request, CancellationToken cancellationToken)
        {
            var report = new ScheduledReport
            {
                TenantId = request.TenantId,
                ReportName = request.ReportName,
                CronExpression = request.CronExpression,
                Recipients = request.Recipients,
                Format = request.Format,
                IsActive = request.IsActive,
                NextRunTime = DateTime.UtcNow.AddDays(1) // Mock next run for demo
            };

            _context.ScheduledReports.Add(report);
            await _context.SaveChangesAsync(cancellationToken);

            return report.Id;
        }
    }
}
