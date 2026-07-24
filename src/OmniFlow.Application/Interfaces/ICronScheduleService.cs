namespace OmniFlow.Application.Interfaces;

/// <summary>
/// Parses standard 5-field Unix cron expressions (e.g. "0 8 * * 1") and computes
/// the next fire time. Kept behind an interface so the Application layer never
/// depends on a specific cron library.
/// </summary>
public interface ICronScheduleService
{
    /// <summary>True if the expression is a valid 5-field cron.</summary>
    bool IsValid(string cronExpression);

    /// <summary>
    /// The next occurrence in UTC strictly after <paramref name="afterUtc"/>, or
    /// null when the expression is invalid or has no future occurrence.
    /// </summary>
    DateTime? GetNextOccurrenceUtc(string cronExpression, DateTime afterUtc);
}
