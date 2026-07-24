namespace OmniFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Well-known identifiers used by the development/demo seed data.
/// These are stable so that seeded rows keep the same identity across databases.
/// </summary>
public static class SeedConstants
{
    public static readonly Guid DefaultTenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    public static readonly Guid HqDepartmentId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid ItServiceDeskId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid ItSupportTemplateId = Guid.Parse("2e3e5c9b-6dbb-4171-aa7a-8cf0c74fb1d2");

    public static readonly Guid RequesterUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AgentUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ApproverUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AdminUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
}
