using OmniFlow.Domain.Common;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities.Approvals;

/// <summary>
/// Names of entities that can be governed by an approval chain. Kept as
/// strings so new modules can plug in without schema changes.
/// </summary>
public static class ApprovalEntityTypes
{
    public const string Ticket = "Ticket";
    public const string LeaveRequest = "LeaveRequest";
}

/// <summary>What a <see cref="ApprovalRequest.Decide"/> call did, so the caller
/// can raise the matching integration event.</summary>
public enum ApprovalDecisionKind
{
    /// <summary>The chain advanced to a new pending step.</summary>
    Advanced,
    /// <summary>The whole chain concluded (see <see cref="ApprovalDecision.IsApproved"/>).</summary>
    Completed
}

public sealed record ApprovalDecision(ApprovalDecisionKind Kind, bool IsApproved, ApprovalStep? NextStep);

/// <summary>
/// Approval chain aggregate root. Owns its steps and the rules for advancing
/// through them: only the current pending step can be decided, the first
/// rejection ends the chain, and approving the last step completes it.
/// Authorization (who may decide) is resolved by the application layer and
/// passed in, since active-delegation lookups need the database.
/// </summary>
public class ApprovalRequest : AuditableEntity
{
    private ApprovalRequest() { }

    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }

    /// <summary>Short human-readable context (ticket title, leave summary).</summary>
    public string EntityDisplay { get; private set; } = string.Empty;

    public Guid RequestedById { get; private set; }
    public ApprovalStatus Status { get; private set; } = ApprovalStatus.Pending;
    public int CurrentStepOrder { get; private set; } = 1;
    public DateTimeOffset? CompletedAt { get; private set; }

    private readonly List<ApprovalStep> _steps = new();
    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    /// <summary>Creates a pending request over pre-resolved steps (ordered from 1).</summary>
    public static ApprovalRequest Create(
        string entityType, Guid entityId, string entityDisplay, Guid requestedById,
        IEnumerable<ApprovalStep> steps)
    {
        var request = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            EntityDisplay = entityDisplay,
            RequestedById = requestedById,
            Status = ApprovalStatus.Pending,
            CurrentStepOrder = 1,
        };

        foreach (var step in steps.OrderBy(s => s.Order))
        {
            step.AttachTo(request);
            request._steps.Add(step);
        }

        if (request._steps.Count == 0)
        {
            throw new ConflictException($"The approval policy for {entityType} has no steps configured.");
        }

        return request;
    }

    /// <summary>The step currently awaiting a decision. Throws if the request is
    /// no longer pending or has no actionable step.</summary>
    public ApprovalStep CurrentStep()
    {
        if (Status != ApprovalStatus.Pending)
        {
            throw new ConflictException($"This approval request has already been {Status}.");
        }

        return _steps.FirstOrDefault(s => s.Order == CurrentStepOrder && s.Status == ApprovalStatus.Pending)
            ?? throw new ConflictException("The approval request has no pending step to decide.");
    }

    /// <summary>
    /// True when the acting user may decide <paramref name="step"/>: they are an
    /// administrator, the named approver, hold the step's role, or hold an active
    /// delegation from the named approver (<paramref name="delegatedApproverIds"/>
    /// is the set of users who delegated their authority to the acting user).
    /// </summary>
    public static bool CanDecide(ApprovalStep step, Guid actingUserId, string actingRole, IReadOnlyCollection<Guid> delegatedApproverIds)
    {
        if (actingRole == "Administrator") return true;
        if (step.ApproverUserId == actingUserId) return true;
        if (step.ApproverRole is not null && step.ApproverRole == actingRole) return true;
        if (step.ApproverUserId.HasValue && delegatedApproverIds.Contains(step.ApproverUserId.Value)) return true;
        return false;
    }

    /// <summary>
    /// Records the decision on the current step and advances the chain: a
    /// rejection concludes the request (remaining steps cancelled); approving the
    /// last step completes it; otherwise the next step becomes current.
    /// </summary>
    public ApprovalDecision Decide(Guid actingUserId, bool approve, string? comment)
    {
        var step = CurrentStep();
        step.Resolve(approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected, actingUserId, comment);

        if (!approve)
        {
            Status = ApprovalStatus.Rejected;
            CompletedAt = DateTimeOffset.UtcNow;
            foreach (var remaining in _steps.Where(s => s.Status == ApprovalStatus.Pending))
            {
                remaining.Cancel();
            }
            return new ApprovalDecision(ApprovalDecisionKind.Completed, IsApproved: false, NextStep: null);
        }

        var nextStep = _steps
            .Where(s => s.Status == ApprovalStatus.Pending)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        if (nextStep is null)
        {
            Status = ApprovalStatus.Approved;
            CompletedAt = DateTimeOffset.UtcNow;
            return new ApprovalDecision(ApprovalDecisionKind.Completed, IsApproved: true, NextStep: null);
        }

        CurrentStepOrder = nextStep.Order;
        return new ApprovalDecision(ApprovalDecisionKind.Advanced, IsApproved: false, NextStep: nextStep);
    }
}

public class ApprovalStep : AuditableEntity
{
    public Guid ApprovalRequestId { get; private set; }
    public ApprovalRequest ApprovalRequest { get; private set; } = null!;

    /// <summary>1-based position in the chain.</summary>
    public int Order { get; private set; }

    public ApproverType ApproverType { get; private set; }

    /// <summary>Identity role allowed to decide, when ApproverType is Role.</summary>
    public string? ApproverRole { get; private set; }

    /// <summary>Concrete approver (SpecificUser, or resolved manager).</summary>
    public Guid? ApproverUserId { get; private set; }

    public ApprovalStatus Status { get; private set; } = ApprovalStatus.Pending;
    public Guid? ActedById { get; private set; }
    public DateTimeOffset? ActedAt { get; private set; }
    public string? Comment { get; private set; }

    private ApprovalStep() { }

    /// <summary>Creates a pending step. Manager resolution (ManagerOfRequester →
    /// a concrete user, or a role fallback) is performed by the caller which has
    /// database access.</summary>
    public static ApprovalStep Create(int order, ApproverType approverType, string? approverRole, Guid? approverUserId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Order = order,
            ApproverType = approverType,
            ApproverRole = approverRole,
            ApproverUserId = approverUserId,
            Status = ApprovalStatus.Pending,
        };

    internal void AttachTo(ApprovalRequest request)
    {
        ApprovalRequest = request;
        ApprovalRequestId = request.Id;
    }

    internal void Resolve(ApprovalStatus outcome, Guid actedById, string? comment)
    {
        Status = outcome;
        ActedById = actedById;
        ActedAt = DateTimeOffset.UtcNow;
        Comment = comment;
    }

    internal void Cancel() => Status = ApprovalStatus.Cancelled;

    /// <summary>Late manager resolution fallback used by the builder when a
    /// requester has no manager on record.</summary>
    public void FallBackToRole(string role)
    {
        ApproverType = ApproverType.Role;
        ApproverRole = role;
        ApproverUserId = null;
    }
}
