using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Tickets.Workflow;

/// <summary>
/// Guards a workflow before it is saved. A malformed workflow (a duplicate
/// trigger from one state, an approval policy on a transition that never
/// reaches WaitingForApproval, an unreachable terminal state) silently breaks
/// ticket operation, so the designer must be prevented from producing one.
/// Pure and self-contained for straightforward unit testing.
/// </summary>
public static class WorkflowValidator
{
    /// <returns>The list of blocking errors; empty means the workflow is valid.</returns>
    public static IReadOnlyList<string> Validate(WorkflowDefinition workflow)
    {
        var errors = new List<string>();
        var transitions = workflow.Transitions ?? new List<WorkflowTransition>();

        if (transitions.Count == 0)
        {
            errors.Add("A workflow needs at least one transition.");
            return errors;
        }

        var seenTriggersPerState = new HashSet<(TicketState, string)>();
        foreach (var (transition, index) in transitions.Select((t, i) => (t, i)))
        {
            var label = $"Transition #{index + 1}";

            if (string.IsNullOrWhiteSpace(transition.Trigger))
            {
                errors.Add($"{label}: a trigger name is required.");
            }
            else if (!seenTriggersPerState.Add((transition.FromState, transition.Trigger.Trim())))
            {
                // The engine matches on (FromState, Trigger), so it must be unique.
                errors.Add($"{label}: '{transition.Trigger}' is already used from state '{transition.FromState}'.");
            }

            if (transition.FromState == transition.ToState)
            {
                errors.Add($"{label}: a transition cannot start and end in '{transition.FromState}'.");
            }

            // An approval chain only makes sense on the way into WaitingForApproval.
            if (transition.ApprovalPolicy is { Steps.Count: > 0 })
            {
                if (transition.ToState != TicketState.WaitingForApproval)
                {
                    errors.Add($"{label}: an approval policy only applies to a transition ending in 'WaitingForApproval'.");
                }
                errors.AddRange(ValidateApprovalPolicy(transition.ApprovalPolicy, label));
            }
        }

        // Terminal states must be reachable or tickets can never be completed.
        if (!Reachable(transitions, TicketState.Resolved) && !Reachable(transitions, TicketState.Closed))
        {
            errors.Add("No transition ever reaches 'Resolved' or 'Closed'; tickets could never be completed.");
        }

        return errors;
    }

    private static IEnumerable<string> ValidateApprovalPolicy(ApprovalDefinition policy, string label)
    {
        var orders = new HashSet<int>();
        foreach (var step in policy.Steps.OrderBy(s => s.Order))
        {
            if (!orders.Add(step.Order))
            {
                yield return $"{label}: approval steps have a duplicate order ({step.Order}).";
            }
            if (step.ApproverType == ApproverType.Role && string.IsNullOrWhiteSpace(step.ApproverRole))
            {
                yield return $"{label}: a role-based approval step needs a role.";
            }
            if (step.ApproverType == ApproverType.SpecificUser && step.ApproverUserId is null)
            {
                yield return $"{label}: a specific-user approval step needs a user.";
            }
        }
    }

    /// <summary>BFS from <see cref="TicketState.New"/> over the transition graph.</summary>
    private static bool Reachable(IReadOnlyList<WorkflowTransition> transitions, TicketState target)
    {
        var visited = new HashSet<TicketState> { TicketState.New };
        var queue = new Queue<TicketState>();
        queue.Enqueue(TicketState.New);

        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            if (state == target) return true;

            foreach (var next in transitions.Where(t => t.FromState == state).Select(t => t.ToState))
            {
                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }
        return false;
    }
}
