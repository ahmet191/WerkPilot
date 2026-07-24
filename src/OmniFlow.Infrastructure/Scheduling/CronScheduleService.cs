using Cronos;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Scheduling;

/// <summary>Cronos-backed implementation of <see cref="ICronScheduleService"/>.</summary>
public class CronScheduleService : ICronScheduleService
{
    public bool IsValid(string cronExpression) => TryParse(cronExpression, out _);

    public DateTime? GetNextOccurrenceUtc(string cronExpression, DateTime afterUtc)
    {
        if (!TryParse(cronExpression, out var expr) || expr is null) return null;
        var after = DateTime.SpecifyKind(afterUtc, DateTimeKind.Utc);
        return expr.GetNextOccurrence(after, TimeZoneInfo.Utc);
    }

    private static bool TryParse(string cronExpression, out CronExpression? expr)
    {
        expr = null;
        if (string.IsNullOrWhiteSpace(cronExpression)) return false;
        try
        {
            expr = CronExpression.Parse(cronExpression.Trim());
            return true;
        }
        catch (CronFormatException)
        {
            return false;
        }
    }
}
