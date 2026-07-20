namespace OmniFlow.Domain.Common;

/// <summary>
/// Roles that exist above the tenant boundary. The permission matrix is
/// per-tenant and therefore cannot express "may create tenants", so platform
/// operations are gated on this role directly.
/// </summary>
public static class PlatformRoles
{
    public const string PlatformAdmin = "PlatformAdmin";

    /// <summary>Authorization policy name for platform-level endpoints.</summary>
    public const string PlatformAdminPolicy = "PlatformAdmin";
}
