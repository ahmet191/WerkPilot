namespace OmniFlow.Domain.Common;

/// <summary>A grantable capability, grouped by module for the admin matrix UI.</summary>
public record PermissionDefinition(string Key, string Module, string DisplayName);

/// <summary>
/// The platform's permission catalog. Permissions — not roles — gate endpoints;
/// each tenant maps its roles onto these keys (see RolePermission), so an
/// operator can re-shape who may do what without a deployment.
/// </summary>
public static class Permissions
{
    // Tickets / service desk
    public const string TicketsView = "tickets.view";
    public const string TicketsCreate = "tickets.create";
    public const string TicketsEdit = "tickets.edit";
    public const string TicketsDelete = "tickets.delete";
    public const string TicketsAssign = "tickets.assign";
    public const string TicketsTransition = "tickets.transition";

    // HR
    public const string LeavesView = "leaves.view";
    public const string LeavesRequest = "leaves.request";
    public const string LeavesApprove = "leaves.approve";
    public const string DirectoryView = "directory.view";

    // Assets
    public const string AssetsView = "assets.view";
    public const string AssetsManage = "assets.manage";

    // Approvals
    public const string ApprovalsDecide = "approvals.decide";

    // Administration
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string AccountsManage = "accounts.manage";
    public const string TemplatesManage = "templates.manage";
    public const string WorkflowsManage = "workflows.manage";
    public const string SlaManage = "sla.manage";
    public const string CategoriesManage = "categories.manage";
    public const string ServiceDesksManage = "servicedesks.manage";
    public const string AuditLogsView = "auditlogs.view";
    public const string PermissionsManage = "permissions.manage";
    public const string TranslationsManage = "translations.manage";
    public const string UiSchemaManage = "uischema.manage";

    public const string ModuleTickets = "Tickets";
    public const string ModuleHr = "Human Resources";
    public const string ModuleAssets = "Assets";
    public const string ModuleApprovals = "Approvals";
    public const string ModuleAdministration = "Administration";

    /// <summary>Every permission the platform knows about, in display order.</summary>
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(TicketsView, ModuleTickets, "View tickets"),
        new(TicketsCreate, ModuleTickets, "Create tickets"),
        new(TicketsEdit, ModuleTickets, "Edit tickets"),
        new(TicketsDelete, ModuleTickets, "Delete tickets"),
        new(TicketsAssign, ModuleTickets, "Assign tickets"),
        new(TicketsTransition, ModuleTickets, "Change ticket state"),

        new(LeavesView, ModuleHr, "View leave requests"),
        new(LeavesRequest, ModuleHr, "Request leave"),
        new(LeavesApprove, ModuleHr, "Approve leave"),
        new(DirectoryView, ModuleHr, "View employee directory"),

        new(AssetsView, ModuleAssets, "View assets"),
        new(AssetsManage, ModuleAssets, "Manage assets"),

        new(ApprovalsDecide, ModuleApprovals, "Decide approvals"),

        new(UsersView, ModuleAdministration, "View users"),
        new(UsersManage, ModuleAdministration, "Create and edit users"),
        new(AccountsManage, ModuleAdministration, "Manage login accounts"),
        new(TemplatesManage, ModuleAdministration, "Manage ticket templates"),
        new(WorkflowsManage, ModuleAdministration, "Manage workflows"),
        new(SlaManage, ModuleAdministration, "Manage SLA policies"),
        new(CategoriesManage, ModuleAdministration, "Manage categories"),
        new(ServiceDesksManage, ModuleAdministration, "Manage service desks"),
        new(AuditLogsView, ModuleAdministration, "View audit logs"),
        new(PermissionsManage, ModuleAdministration, "Manage the permission matrix"),
        new(TranslationsManage, ModuleAdministration, "Customize interface text"),
        new(UiSchemaManage, ModuleAdministration, "Customize screen layouts"),
    ];

    private static readonly HashSet<string> Keys = All.Select(p => p.Key).ToHashSet(StringComparer.Ordinal);

    public static bool IsKnown(string permission) => Keys.Contains(permission);

    /// <summary>
    /// The role that always holds every permission. Excluded from the matrix so
    /// an operator can never lock themselves out of the platform.
    /// </summary>
    public const string SuperRole = "Administrator";

    /// <summary>Default grants applied to a new tenant.</summary>
    public static IReadOnlyDictionary<string, string[]> DefaultMatrix { get; } =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["ITAgent"] =
            [
                TicketsView, TicketsCreate, TicketsEdit, TicketsAssign, TicketsTransition,
                AssetsView, AssetsManage, DirectoryView, ApprovalsDecide,
            ],
            ["HRAdmin"] =
            [
                TicketsView, TicketsCreate,
                LeavesView, LeavesRequest, LeavesApprove, DirectoryView,
                UsersView, ApprovalsDecide,
            ],
            ["Requester"] =
            [
                TicketsView, TicketsCreate,
                LeavesView, LeavesRequest, DirectoryView, AssetsView,
            ],
        };
}
