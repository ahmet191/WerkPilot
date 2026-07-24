using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Common;

/// <summary>
/// JSONB value object describing an approval chain template, in the same style
/// as <c>FieldDefinition</c>/<c>WorkflowDefinition</c>. Lives either on a
/// workflow transition (ticket approvals) or on the tenant (leave approvals).
/// Steps are materialized into concrete <c>ApprovalStep</c> rows when a
/// request is created.
/// </summary>
public class ApprovalDefinition
{
    public List<ApprovalStepDefinition> Steps { get; set; } = new();
}

public class ApprovalStepDefinition
{
    public int Order { get; set; }
    public ApproverType ApproverType { get; set; }

    /// <summary>Identity role name (e.g. "HRAdmin") when ApproverType is Role.</summary>
    public string? ApproverRole { get; set; }

    /// <summary>Target user when ApproverType is SpecificUser.</summary>
    public Guid? ApproverUserId { get; set; }
}
