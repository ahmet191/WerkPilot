using OmniFlow.Domain.Common;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// Leave request aggregate. Created Pending; the outcome is applied through
/// <see cref="Approve"/>/<see cref="Reject"/>, which reject a request that has
/// already been decided.
/// </summary>
public class LeaveRequest : AuditableEntity
{
    private LeaveRequest() { }

    public Guid UserId { get; private set; }
    public LeaveType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public LeaveStatus Status { get; private set; }
    public Guid? ApproverId { get; private set; }
    public string? Reason { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;
    public User? Approver { get; private set; }

    /// <summary>Creates a Pending leave request. Date ordering is validated by
    /// the command validator; dates are normalized to UTC for Npgsql.</summary>
    public static LeaveRequest Create(Guid userId, LeaveType type, DateTime startDate, DateTime endDate, string? reason) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            // Bare dates deserialize with Kind=Unspecified; Npgsql only accepts
            // UTC for 'timestamp with time zone' columns.
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            Status = LeaveStatus.Pending,
            Reason = reason,
        };

    public void Approve(Guid approverId) => Resolve(LeaveStatus.Approved, approverId);

    public void Reject(Guid approverId) => Resolve(LeaveStatus.Rejected, approverId);

    /// <summary>Applies an approve/reject outcome (used by both the direct
    /// approve command and the approval-engine consumer).</summary>
    public void Resolve(LeaveStatus outcome, Guid approverId)
    {
        if (Status != LeaveStatus.Pending)
        {
            throw new ConflictException($"This leave request has already been {Status}.");
        }
        if (outcome is not (LeaveStatus.Approved or LeaveStatus.Rejected))
        {
            throw new ConflictException($"'{outcome}' is not a valid decision for a leave request.");
        }

        Status = outcome;
        ApproverId = approverId;
    }
}
