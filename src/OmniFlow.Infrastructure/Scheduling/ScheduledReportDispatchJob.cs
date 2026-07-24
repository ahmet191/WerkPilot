using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Persistence;
using Quartz;

namespace OmniFlow.Infrastructure.Scheduling;

/// <summary>
/// Runs due scheduled reports: for every active report whose next run time has
/// passed, it composes a tenant-scoped service-desk summary, emails it to the
/// configured recipients, and advances the next run to the following cron
/// occurrence. Cross-tenant by design (IgnoreQueryFilters) — each report carries
/// its own TenantId, which scopes the data it summarises. A failure on one
/// report is logged and skipped so it never blocks the others.
/// </summary>
[DisallowConcurrentExecution]
public class ScheduledReportDispatchJob : IJob
{
    public static readonly JobKey Key = new("scheduled-report-dispatch", "omniflow");

    private readonly OmniFlowDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ICronScheduleService _cron;
    private readonly ILogger<ScheduledReportDispatchJob> _logger;

    public ScheduledReportDispatchJob(
        OmniFlowDbContext dbContext,
        IEmailSender emailSender,
        ICronScheduleService cron,
        ILogger<ScheduledReportDispatchJob> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _cron = cron;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = DateTime.UtcNow;
        var ct = context.CancellationToken;

        var due = await _dbContext.ScheduledReports
            .IgnoreQueryFilters()
            .Where(r => !r.IsDeleted && r.IsActive && r.NextRunTime != null && r.NextRunTime <= now)
            .Take(50)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        _logger.LogInformation("Dispatching {Count} scheduled report(s).", due.Count);

        foreach (var report in due)
        {
            try
            {
                var html = await BuildReportHtmlAsync(report.TenantId, report.ReportName, now, ct);

                var recipients = report.Recipients
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var to in recipients)
                {
                    await _emailSender.SendAsync(to, $"[WerkPilot] {report.ReportName}", html, ct);
                }

                report.LastRunTime = now;
                var next = _cron.GetNextOccurrenceUtc(report.CronExpression, now);
                if (next is null)
                {
                    report.IsActive = false;
                    report.NextRunTime = null;
                    _logger.LogWarning("Report {ReportId} has an invalid cron and was paused.", report.Id);
                }
                else
                {
                    report.NextRunTime = next;
                }

                _logger.LogInformation("Report {ReportId} '{Name}' sent; next run {Next}.",
                    report.Id, report.ReportName, report.NextRunTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch report {ReportId}; advancing to avoid a tight loop.", report.Id);
                var nextFallback = _cron.GetNextOccurrenceUtc(report.CronExpression, now) ?? now.AddHours(1);
                report.NextRunTime = nextFallback;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<string> BuildReportHtmlAsync(Guid tenantId, string reportName, DateTime asOfUtc, CancellationToken ct)
    {
        var tickets = await _dbContext.Tickets
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted)
            .Select(t => new { t.CurrentState, HasBreach = t.SlaBreachNotifiedAt != null })
            .ToListAsync(ct);

        var total = tickets.Count;
        var open = tickets.Count(t => t.CurrentState != TicketState.Resolved && t.CurrentState != TicketState.Closed);
        var breached = tickets.Count(t => t.HasBreach);
        var compliancePct = total == 0 ? 100 : Math.Round((double)(total - breached) / total * 100, 1);

        var byStatus = tickets
            .GroupBy(t => t.CurrentState)
            .OrderByDescending(g => g.Count())
            .Select(g => $"<tr><td style='padding:4px 12px;border:1px solid #eee'>{g.Key}</td><td style='padding:4px 12px;border:1px solid #eee;text-align:right'>{g.Count()}</td></tr>");

        var sb = new StringBuilder();
        sb.Append("<div style='font-family:Segoe UI,Arial,sans-serif;color:#1f2937'>");
        sb.Append($"<h2 style='margin:0 0 4px'>{reportName}</h2>");
        sb.Append($"<p style='color:#6b7280;margin:0 0 16px'>Generated {asOfUtc:yyyy-MM-dd HH:mm} UTC</p>");
        sb.Append("<table style='border-collapse:collapse;margin-bottom:16px'>");
        sb.Append($"<tr><td style='padding:4px 12px'>Total tickets</td><td style='padding:4px 12px;text-align:right'><b>{total}</b></td></tr>");
        sb.Append($"<tr><td style='padding:4px 12px'>Open</td><td style='padding:4px 12px;text-align:right'><b>{open}</b></td></tr>");
        sb.Append($"<tr><td style='padding:4px 12px'>SLA breaches</td><td style='padding:4px 12px;text-align:right'><b>{breached}</b></td></tr>");
        sb.Append($"<tr><td style='padding:4px 12px'>SLA compliance</td><td style='padding:4px 12px;text-align:right'><b>{compliancePct}%</b></td></tr>");
        sb.Append("</table>");
        sb.Append("<h3 style='margin:0 0 8px'>Tickets by status</h3>");
        sb.Append("<table style='border-collapse:collapse'>");
        sb.Append("<tr><th style='padding:4px 12px;border:1px solid #eee;text-align:left'>Status</th><th style='padding:4px 12px;border:1px solid #eee'>Count</th></tr>");
        sb.Append(string.Concat(byStatus));
        sb.Append("</table>");
        sb.Append("<p style='color:#9ca3af;font-size:12px;margin-top:20px'>Automated report from WerkPilot ESM.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }
}
