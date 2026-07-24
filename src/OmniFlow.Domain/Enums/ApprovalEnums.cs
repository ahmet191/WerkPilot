namespace OmniFlow.Domain.Enums;

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public enum ApproverType
{
    /// <summary>Any user holding the configured Identity role may decide.</summary>
    Role,

    /// <summary>A specific user (or their active delegate) must decide.</summary>
    SpecificUser,

    /// <summary>Resolved to the requester's manager when the request is created.</summary>
    ManagerOfRequester
}
