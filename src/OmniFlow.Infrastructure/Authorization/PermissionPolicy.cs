using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace OmniFlow.Infrastructure.Authorization;

/// <summary>
/// Requirement carrying the permission key an endpoint demands.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}

/// <summary>
/// Naming convention bridging attributes and the matrix:
/// <c>[Authorize(Policy = PermissionPolicy.For(Permissions.TicketsAssign))]</c>.
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "perm:";
    public static string For(string permission) => Prefix + permission;
}

/// <summary>
/// Compile-time policy names for <c>[Authorize(Policy = ...)]</c>, which cannot
/// call <see cref="PermissionPolicy.For"/>. Keep in sync with
/// <see cref="OmniFlow.Domain.Common.Permissions"/>.
/// </summary>
public static class PermissionPolicyNames
{
    private const string P = PermissionPolicy.Prefix;

    public const string TicketsView = P + "tickets.view";
    public const string TicketsCreate = P + "tickets.create";
    public const string TicketsEdit = P + "tickets.edit";
    public const string TicketsDelete = P + "tickets.delete";
    public const string TicketsAssign = P + "tickets.assign";
    public const string TicketsTransition = P + "tickets.transition";

    public const string LeavesView = P + "leaves.view";
    public const string LeavesApprove = P + "leaves.approve";
    public const string DirectoryView = P + "directory.view";

    public const string AssetsManage = P + "assets.manage";
    public const string ApprovalsDecide = P + "approvals.decide";

    public const string UsersView = P + "users.view";
    public const string UsersManage = P + "users.manage";
    public const string AccountsManage = P + "accounts.manage";
    public const string TemplatesManage = P + "templates.manage";
    public const string WorkflowsManage = P + "workflows.manage";
    public const string SlaManage = P + "sla.manage";
    public const string CategoriesManage = P + "categories.manage";
    public const string ServiceDesksManage = P + "servicedesks.manage";
    public const string AuditLogsView = P + "auditlogs.view";
    public const string PermissionsManage = P + "permissions.manage";
}

/// <summary>
/// Materializes a policy for any "perm:*" name on demand, so permissions can be
/// added without touching startup registration. Everything else (the legacy
/// role policies, the fallback policy) defers to the default provider.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicy.Prefix, StringComparison.Ordinal))
        {
            var permission = policyName[PermissionPolicy.Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
