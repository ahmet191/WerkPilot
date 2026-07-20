namespace OmniFlow.Domain.Enums;

/// <summary>Category of a notification — drives the icon and colour in the UI.</summary>
public enum NotificationKind
{
    General = 0,
    TicketCreated = 1,
    TicketAssigned = 2,
    TicketStateChanged = 3,
    SlaBreached = 4,
    ApprovalRequested = 5,
    ApprovalDecided = 6,
    LeaveDecision = 7,
    AssetAssigned = 8,
}
