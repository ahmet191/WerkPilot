using OmniFlow.Application.Tickets.Workflow;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using Xunit;

namespace OmniFlow.Application.UnitTests.Workflow;

public class WorkflowValidatorTests
{
    private static WorkflowTransition T(TicketState from, TicketState to, string trigger,
        UserRole? role = null, ApprovalDefinition? policy = null)
        => new() { FromState = from, ToState = to, Trigger = trigger, RequiredRole = role, ApprovalPolicy = policy };

    [Fact]
    public void Valid_linear_workflow_has_no_errors()
    {
        var workflow = new WorkflowDefinition
        {
            Transitions =
            {
                T(TicketState.New, TicketState.InProgress, "StartWork", UserRole.Agent),
                T(TicketState.InProgress, TicketState.Resolved, "Resolve", UserRole.Agent),
                T(TicketState.Resolved, TicketState.Closed, "Close"),
            }
        };

        Assert.Empty(WorkflowValidator.Validate(workflow));
    }

    [Fact]
    public void Empty_workflow_is_rejected()
    {
        var errors = WorkflowValidator.Validate(new WorkflowDefinition());
        Assert.Contains(errors, e => e.Contains("at least one transition"));
    }

    [Fact]
    public void Duplicate_trigger_from_same_state_is_rejected()
    {
        var workflow = new WorkflowDefinition
        {
            Transitions =
            {
                T(TicketState.New, TicketState.InProgress, "Go"),
                T(TicketState.New, TicketState.Resolved, "Go"), // same (state, trigger)
            }
        };

        var errors = WorkflowValidator.Validate(workflow);
        Assert.Contains(errors, e => e.Contains("already used from state"));
    }

    [Fact]
    public void Self_transition_is_rejected()
    {
        var workflow = new WorkflowDefinition
        {
            Transitions = { T(TicketState.InProgress, TicketState.InProgress, "Loop") }
        };

        var errors = WorkflowValidator.Validate(workflow);
        Assert.Contains(errors, e => e.Contains("cannot start and end"));
    }

    [Fact]
    public void Unreachable_terminal_states_are_rejected()
    {
        // New → InProgress and back, never reaching Resolved/Closed.
        var workflow = new WorkflowDefinition
        {
            Transitions =
            {
                T(TicketState.New, TicketState.InProgress, "Start"),
                T(TicketState.InProgress, TicketState.WaitingForApproval, "Hold"),
            }
        };

        var errors = WorkflowValidator.Validate(workflow);
        Assert.Contains(errors, e => e.Contains("reaches 'Resolved'"));
    }

    [Fact]
    public void Approval_policy_off_the_approval_state_is_rejected()
    {
        var policy = new ApprovalDefinition
        {
            Steps = { new ApprovalStepDefinition { Order = 1, ApproverType = ApproverType.Role, ApproverRole = "HRAdmin" } }
        };
        var workflow = new WorkflowDefinition
        {
            Transitions =
            {
                // Approval policy but target is Resolved, not WaitingForApproval.
                T(TicketState.New, TicketState.Resolved, "Approve", policy: policy),
            }
        };

        var errors = WorkflowValidator.Validate(workflow);
        Assert.Contains(errors, e => e.Contains("WaitingForApproval"));
    }

    [Fact]
    public void Role_approval_step_without_a_role_is_rejected()
    {
        var policy = new ApprovalDefinition
        {
            Steps = { new ApprovalStepDefinition { Order = 1, ApproverType = ApproverType.Role, ApproverRole = null } }
        };
        var workflow = new WorkflowDefinition
        {
            Transitions =
            {
                T(TicketState.New, TicketState.WaitingForApproval, "RequestApproval", policy: policy),
                T(TicketState.WaitingForApproval, TicketState.Resolved, "Approve"),
            }
        };

        var errors = WorkflowValidator.Validate(workflow);
        Assert.Contains(errors, e => e.Contains("needs a role"));
    }
}
