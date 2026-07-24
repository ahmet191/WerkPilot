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
                .OrderByDescending(r => r.CreatedAt)
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
        private readonly ICronScheduleService _cron;

        public CreateScheduledReportCommandHandler(IOmniFlowDbContext context, ICronScheduleService cron)
        {
            _context = context;
            _cron = cron;
        }

        public async Task<Guid> Handle(CreateScheduledReportCommand request, CancellationToken cancellationToken)
        {
            // The next run is derived from the cron expression, not a fixed offset,
            // so the dispatch job fires it at the right time. An unparseable cron is
            // a 400 rather than a silently-never-running report.
            var nextRun = _cron.GetNextOccurrenceUtc(request.CronExpression, DateTime.UtcNow);
            if (nextRun is null)
            {
                throw new OmniFlow.Domain.Common.Exceptions.ValidationException(
                    nameof(request.CronExpression),
                    "Invalid cron expression. Use 5-field Unix cron, e.g. '0 8 * * 1' (every Monday at 08:00).");
            }

            var report = new ScheduledReport
            {
                TenantId = request.TenantId,
                ReportName = request.ReportName,
                CronExpression = request.CronExpression.Trim(),
                Recipients = request.Recipients,
                Format = request.Format,
                IsActive = request.IsActive,
                NextRunTime = nextRun
            };

            _context.ScheduledReports.Add(report);
            await _context.SaveChangesAsync(cancellationToken);

            return report.Id;
        }
    }
}
