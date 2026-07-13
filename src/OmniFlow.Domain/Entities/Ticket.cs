using System.Text.RegularExpressions;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// Ticket aggregate root. State changes, dynamic-field validation, assignment
/// and commenting are expressed as behavior methods that enforce the domain
/// invariants — callers never mutate state directly. Audit-log entries are
/// produced by the aggregate as part of each operation.
/// </summary>
public class Ticket : AuditableEntity
{
    // EF materialization constructor.
    private Ticket() { }

    public Guid TemplateId { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? CategoryId { get; set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketState CurrentState { get; private set; }

    // ITSM Fields
    public TicketType Type { get; private set; } = TicketType.Incident;
    public TicketPriority Priority { get; private set; } = TicketPriority.Medium;
    public TicketImpact Impact { get; private set; } = TicketImpact.Medium;
    public TicketUrgency Urgency { get; private set; } = TicketUrgency.Medium;

    public string? ResolutionNotes { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public Guid? ServiceDeskId { get; set; }
    public ServiceDesk? ServiceDesk { get; set; }

    // CMDB & Problem Management References
    public Guid? ConfigurationItemId { get; set; }
    public Guid? ProblemId { get; set; }
    public ConfigurationItem? ConfigurationItem { get; set; }
    public Problem? Problem { get; set; }

    // SLAs
    public DateTimeOffset? SlaDueDate { get; private set; }
    public DateTimeOffset? ResolutionDate { get; private set; }

    /// <summary>Set once the breach alert has been sent — keeps the SLA sweep idempotent.</summary>
    public DateTimeOffset? SlaBreachNotifiedAt { get; set; }

    // Dynamic properties mapped via JSON conversion
    public Dictionary<string, object> DynamicData { get; private set; } = new();

    public TicketTemplate Template { get; private set; } = null!;
    public User Requester { get; private set; } = null!;
    public User? Assignee { get; private set; }
    public TicketCategory? Category { get; set; }

    private readonly List<TicketComment> _comments = new();
    public IReadOnlyCollection<TicketComment> Comments => _comments.AsReadOnly();

    private readonly List<TicketAuditLog> _auditLogs = new();
    public IReadOnlyCollection<TicketAuditLog> AuditLogs => _auditLogs.AsReadOnly();

    /// <summary>
    /// Creates a ticket from a template, validating the submitted dynamic data
    /// against the template's field definitions (presence, dropdown options,
    /// regex) and lifting the well-known fields (Title/Type/Priority/…) out of
    /// the payload. Starts in <see cref="TicketState.New"/> with a Create audit.
    /// </summary>
    public static Ticket Create(TicketTemplate template, Guid requesterId, Dictionary<string, object> dynamicData)
    {
        ValidateDynamicFields(template, dynamicData);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            RequesterId = requesterId,
            CurrentState = TicketState.New,
            DynamicData = dynamicData,
            Title = ReadString(dynamicData, "Title", "Untitled Ticket"),
            Description = ReadString(dynamicData, "Description", string.Empty),
            Type = ReadEnum(dynamicData, "Type", TicketType.Incident),
            Priority = ReadEnum(dynamicData, "Priority", TicketPriority.Medium),
            Impact = ReadEnum(dynamicData, "Impact", TicketImpact.Medium),
            Urgency = ReadEnum(dynamicData, "Urgency", TicketUrgency.Medium),
        };

        ticket.AddAudit(requesterId, "Create", "Ticket",
            newValue: $"Created ticket with Template: {template.LocalizedName.GetValueOrDefault("en")}");

        return ticket;
    }

    /// <summary>
    /// Applies a workflow trigger. Enforces that the trigger is valid from the
    /// current state, that a user-fired transition out of WaitingForApproval is
    /// rejected (those are decided by the approval engine), and the required
    /// role. Returns the matched transition so the caller can act on its
    /// approval policy. Set <paramref name="isSystem"/> for platform-initiated
    /// transitions (e.g. the approval-completed consumer), which bypass the
    /// approval-guard and role checks.
    /// </summary>
    public WorkflowTransition Transition(string trigger, User actingUser, bool isSystem = false)
    {
        var transitions = Template.Workflow?.Transitions;
        if (transitions is null || transitions.Count == 0)
        {
            throw new ConflictException("The ticket template has no workflow transitions configured.");
        }

        var previousState = CurrentState;
        var match = transitions.FirstOrDefault(t => t.FromState == previousState && t.Trigger == trigger)
            ?? throw new ConflictException($"Transition '{trigger}' is not configured from state '{previousState}'.");

        if (previousState == TicketState.WaitingForApproval && !isSystem)
        {
            throw new ForbiddenAccessException(
                "This ticket is pending approval. Decisions are made from the Approvals screen, not by direct transition.");
        }

        if (!isSystem &&
            match.RequiredRole.HasValue &&
            match.RequiredRole.Value != actingUser.Role &&
            actingUser.Role != UserRole.Administrator)
        {
            throw new ForbiddenAccessException(
                $"Trigger '{trigger}' requires the '{match.RequiredRole}' role. Your role is '{actingUser.Role}'.");
        }

        CurrentState = match.ToState;
        if (CurrentState == TicketState.Resolved)
        {
            ResolutionDate = DateTimeOffset.UtcNow;
        }

        AddAudit(actingUser.Id, "Transition", nameof(CurrentState),
            oldValue: previousState.ToString(), newValue: CurrentState.ToString());

        return match;
    }

    /// <summary>Assigns (or clears) the ticket's assignee.</summary>
    public void Assign(Guid? assigneeId, Guid actingUserId)
    {
        var previous = AssigneeId;
        AssigneeId = assigneeId;
        AddAudit(actingUserId, "Assign", nameof(AssigneeId),
            oldValue: previous?.ToString(), newValue: assigneeId?.ToString());
    }

    /// <summary>Adds a comment authored by a user, with an accompanying audit entry.</summary>
    public TicketComment AddComment(Guid authorId, string content)
    {
        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = Id,
            AuthorId = authorId,
            Content = content,
        };
        _comments.Add(comment);
        AddAudit(authorId, "AddComment", "Comment", newValue: content);
        return comment;
    }

    private void AddAudit(Guid changedById, string action, string fieldName, string? oldValue = null, string? newValue = null)
    {
        _auditLogs.Add(new TicketAuditLog
        {
            Id = Guid.NewGuid(),
            TicketId = Id,
            ChangedById = changedById,
            ChangedAt = DateTimeOffset.UtcNow,
            Action = action,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
        });
    }

    // ── dynamic-field helpers ───────────────────────────────────────────

    private static void ValidateDynamicFields(TicketTemplate template, Dictionary<string, object> data)
    {
        foreach (var field in template.Fields)
        {
            var hasValue = data.TryGetValue(field.Key, out var value);

            if (field.IsRequired && (!hasValue || value is null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                throw new ValidationException(field.Key, $"Required custom field '{field.Key}' is missing or empty.");
            }

            if (!hasValue || value is null) continue;
            var text = value.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) continue;

            if (field.DropdownOptions is { Count: > 0 } && !field.DropdownOptions.Contains(text))
            {
                throw new ValidationException(field.Key,
                    $"Value '{text}' is invalid. Allowed options: {string.Join(", ", field.DropdownOptions)}");
            }

            if (!string.IsNullOrEmpty(field.ValidationRegex) && !Regex.IsMatch(text, field.ValidationRegex))
            {
                throw new ValidationException(field.Key,
                    $"Value '{text}' does not match the required pattern: '{field.ValidationRegex}'");
            }
        }
    }

    private static string ReadString(Dictionary<string, object> data, string key, string fallback) =>
        data.TryGetValue(key, out var v) ? v.ToString() ?? fallback : fallback;

    private static TEnum ReadEnum<TEnum>(Dictionary<string, object> data, string key, TEnum fallback) where TEnum : struct =>
        data.TryGetValue(key, out var v) && Enum.TryParse<TEnum>(v.ToString(), out var parsed) ? parsed : fallback;
}
