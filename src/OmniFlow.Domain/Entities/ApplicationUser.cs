using Microsoft.AspNetCore.Identity;
using System;

namespace OmniFlow.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    // Additional profile fields can be added here
}
