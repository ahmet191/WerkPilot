using Microsoft.AspNetCore.Identity;

namespace OmniFlow.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity account. Lives in Infrastructure (not Domain) so the
/// Domain layer carries no Identity framework dependency.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    // Additional profile fields can be added here
}
