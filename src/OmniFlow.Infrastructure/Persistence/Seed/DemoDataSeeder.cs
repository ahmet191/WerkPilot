using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Rich, realistic demo dataset — the platform as a 10-person company has been
/// using it: five departments, IT and HR service desks with their own templates
/// and categories, ten people, a fleet of assets, and a backlog of tickets,
/// leave requests and expense claims across many states.
///
/// Every block is idempotent on its own fixed identifiers, so this imports once
/// on a fresh database, fills the gaps on an existing one, and never duplicates —
/// safe to run on every boot. Runs after <see cref="TenantBaseline"/> and the
/// legacy demo users, and reuses their well-known ids where they already exist.
/// </summary>
public static class DemoDataSeeder
{
    // ── Departments ─────────────────────────────────────────────────────
    private static readonly Guid ItDeptId = Guid.Parse("d1000000-0000-0000-0000-000000000001");
    private static readonly Guid HrDeptId = Guid.Parse("d1000000-0000-0000-0000-000000000002");
    private static readonly Guid FinanceDeptId = Guid.Parse("d1000000-0000-0000-0000-000000000003");
    private static readonly Guid SalesDeptId = Guid.Parse("d1000000-0000-0000-0000-000000000004");
    private static readonly Guid OpsDeptId = Guid.Parse("d1000000-0000-0000-0000-000000000005");

    // ── Service desks & HR template ─────────────────────────────────────
    private static readonly Guid HrServiceDeskId = Guid.Parse("d2000000-0000-0000-0000-000000000001");
    private static readonly Guid HrTemplateId = Guid.Parse("d2000000-0000-0000-0000-000000000010");

    // ── Categories ──────────────────────────────────────────────────────
    private static readonly Guid CatHardwareId = Guid.Parse("d3000000-0000-0000-0000-000000000001");
    private static readonly Guid CatSoftwareId = Guid.Parse("d3000000-0000-0000-0000-000000000002");
    private static readonly Guid CatNetworkId = Guid.Parse("d3000000-0000-0000-0000-000000000003");
    private static readonly Guid CatAccessId = Guid.Parse("d3000000-0000-0000-0000-000000000004");
    private static readonly Guid CatPayrollId = Guid.Parse("d3000000-0000-0000-0000-000000000005");
    private static readonly Guid CatOnboardId = Guid.Parse("d3000000-0000-0000-0000-000000000006");
    private static readonly Guid CatBenefitsId = Guid.Parse("d3000000-0000-0000-0000-000000000007");
    private static readonly Guid CatLeaveQId = Guid.Parse("d3000000-0000-0000-0000-000000000008");

    // ── New people (the existing four keep their SeedConstants ids) ──────
    private static readonly Guid ItManagerId = Guid.Parse("d4000000-0000-0000-0000-000000000001"); // İbrahim
    private static readonly Guid ItAgent2Id = Guid.Parse("d4000000-0000-0000-0000-000000000002");  // Diana
    private static readonly Guid HrSpecialistId = Guid.Parse("d4000000-0000-0000-0000-000000000003"); // Emre
    private static readonly Guid FinanceEmpId = Guid.Parse("d4000000-0000-0000-0000-000000000004"); // Fatma
    private static readonly Guid SalesEmpId = Guid.Parse("d4000000-0000-0000-0000-000000000005");   // Gökhan
    private static readonly Guid OpsEmpId = Guid.Parse("d4000000-0000-0000-0000-000000000006");     // Hande

    public static async Task SeedAsync(OmniFlowDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedDepartmentsAsync(context, cancellationToken);
        await SeedCategoriesAsync(context, cancellationToken);
        await SeedHrDeskAndTemplateAsync(context, cancellationToken);
        await SeedPeopleAsync(context, cancellationToken);
        await SeedDirectoryProfilesAsync(context, cancellationToken);
        await SeedAssetsAsync(context, cancellationToken);
        await SeedTicketsAsync(context, cancellationToken);
        await SeedLeaveAsync(context, cancellationToken);
        await SeedExpensesAsync(context, cancellationToken);
        await SpecializeMenusAsync(context, cancellationToken);
    }

    // ── Departments ─────────────────────────────────────────────────────
    private static async Task SeedDepartmentsAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (await context.Departments.AnyAsync(d => d.Id == ItDeptId, ct)) return;

        Department Dept(Guid id, string en, string tr, string cost) => new()
        {
            Id = id,
            TenantId = SeedConstants.DefaultTenantId,
            LocalizedName = new MultiLingualString { Translations = new() { ["en"] = en, ["tr"] = tr } },
            CostCenterCode = cost,
        };

        context.Departments.AddRange(
            Dept(ItDeptId, "Information Technology", "Bilgi Teknolojileri", "CC-IT-100"),
            Dept(HrDeptId, "Human Resources", "İnsan Kaynakları", "CC-HR-200"),
            Dept(FinanceDeptId, "Finance", "Finans", "CC-FIN-300"),
            Dept(SalesDeptId, "Sales", "Satış", "CC-SAL-400"),
            Dept(OpsDeptId, "Operations", "Operasyon", "CC-OPS-500"));
        await context.SaveChangesAsync(ct);
    }

    // ── Ticket categories (IT vs HR, each owned by its department) ───────
    private static async Task SeedCategoriesAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (await context.TicketCategories.AnyAsync(c => c.Id == CatHardwareId, ct)) return;

        TicketCategory Cat(Guid id, string en, string tr, Guid dept) => new()
        {
            Id = id,
            TenantId = SeedConstants.DefaultTenantId,
            DepartmentId = dept,
            LocalizedName = new MultiLingualString { Translations = new() { ["en"] = en, ["tr"] = tr } },
        };

        context.TicketCategories.AddRange(
            Cat(CatHardwareId, "Hardware", "Donanım", ItDeptId),
            Cat(CatSoftwareId, "Software", "Yazılım", ItDeptId),
            Cat(CatNetworkId, "Network", "Ağ", ItDeptId),
            Cat(CatAccessId, "Access Management", "Erişim Yönetimi", ItDeptId),
            Cat(CatPayrollId, "Payroll", "Bordro", HrDeptId),
            Cat(CatOnboardId, "Onboarding", "İşe Alım", HrDeptId),
            Cat(CatBenefitsId, "Benefits", "Yan Haklar", HrDeptId),
            Cat(CatLeaveQId, "Leave Query", "İzin Sorusu", HrDeptId));
        await context.SaveChangesAsync(ct);
    }

    // ── HR service desk + HR request template (separate from IT) ─────────
    private static async Task SeedHrDeskAndTemplateAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (await context.ServiceDesks.AnyAsync(sd => sd.Id == HrServiceDeskId, ct)) return;

        context.ServiceDesks.Add(new ServiceDesk
        {
            Id = HrServiceDeskId,
            TenantId = SeedConstants.DefaultTenantId,
            Name = "HR Service Desk",
            Description = "People operations: payroll, benefits, onboarding and leave queries.",
            DepartmentId = HrDeptId,
            IsPublicToAllEmployees = true,
        });

        // Mirrors the IT template's well-known fields (so seeded dynamic data
        // validates identically) plus an HR-specific request type.
        string[] hrTypes = { "Payroll", "Onboarding", "Benefits", "LeaveQuery", "Other" };
        context.TicketTemplates.Add(new TicketTemplate
        {
            Id = HrTemplateId,
            TenantId = SeedConstants.DefaultTenantId,
            ServiceDeskId = HrServiceDeskId,
            LocalizedName = new MultiLingualString { Translations = new() { ["en"] = "HR Request", ["tr"] = "İK Talebi" } },
            Fields = new List<FieldDefinition>
            {
                new() { Key = "Title", LocalizedName = new MultiLingualString("en", "Subject"), Type = "Text", IsRequired = true, ValidationRegex = "^.{5,150}$" },
                new() { Key = "RequestType", LocalizedName = new MultiLingualString("en", "Request Type"), Type = "Dropdown", IsRequired = true, DropdownOptions = hrTypes.ToList() },
                new() { Key = "Type", LocalizedName = new MultiLingualString("en", "Type"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Incident", "ServiceRequest", "Problem", "Change" } },
                new() { Key = "Priority", LocalizedName = new MultiLingualString("en", "Priority"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High", "Critical" } },
                new() { Key = "Impact", LocalizedName = new MultiLingualString("en", "Impact"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High" } },
                new() { Key = "Urgency", LocalizedName = new MultiLingualString("en", "Urgency"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High" } },
                new() { Key = "Description", LocalizedName = new MultiLingualString("en", "Description"), Type = "Text", IsRequired = false },
            },
            // Same trigger set as the IT template, so one drive helper handles both.
            Workflow = new WorkflowDefinition
            {
                Transitions = new List<WorkflowTransition>
                {
                    new() { FromState = TicketState.New, ToState = TicketState.InProgress, Trigger = "StartWork", RequiredRole = UserRole.Agent },
                    new() { FromState = TicketState.InProgress, ToState = TicketState.WaitingForApproval, Trigger = "RequestApproval", RequiredRole = UserRole.Agent },
                    new() { FromState = TicketState.WaitingForApproval, ToState = TicketState.Resolved, Trigger = "Approve", RequiredRole = UserRole.Approver },
                    new() { FromState = TicketState.WaitingForApproval, ToState = TicketState.InProgress, Trigger = "Reject", RequiredRole = UserRole.Approver },
                    new() { FromState = TicketState.Resolved, ToState = TicketState.Closed, Trigger = "Close", RequiredRole = UserRole.Requester },
                }
            }
        });
        await context.SaveChangesAsync(ct);
    }

    // ── People: six new colleagues + reassign the existing four to depts ─
    private static async Task SeedPeopleAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (!await context.Users.AnyAsync(u => u.Id == ItManagerId, ct))
        {
            User Person(Guid id, string name, string email, UserRole role, string title, Guid dept,
                Guid? manager, Gender gender, string phone) => new()
            {
                Id = id,
                Name = name,
                Email = email,
                Role = role,
                Title = title,
                DepartmentId = dept,
                ManagerId = manager,
                Gender = gender,
                PhoneNumber = phone,
                PersonalEmail = email.Replace("@omniflow.com", "@gmail.com"),
                Nationality = "Türkiye",
                DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 1, 1).AddDays(id.GetHashCode() % 4000), DateTimeKind.Utc),
            };

            context.Users.AddRange(
                Person(ItManagerId, "İbrahim Çelik", "ibrahim@omniflow.com", UserRole.Agent, "IT Manager", ItDeptId, null, Gender.Male, "+90 532 100 1001"),
                Person(ItAgent2Id, "Diana Yıldız", "diana@omniflow.com", UserRole.Agent, "Senior Systems Engineer", ItDeptId, ItManagerId, Gender.Female, "+90 532 100 1002"),
                Person(HrSpecialistId, "Emre Kaya", "emre@omniflow.com", UserRole.Agent, "HR Specialist", HrDeptId, null, Gender.Male, "+90 532 100 1003"),
                Person(FinanceEmpId, "Fatma Demir", "fatma@omniflow.com", UserRole.Requester, "Accountant", FinanceDeptId, null, Gender.Female, "+90 532 100 1004"),
                Person(SalesEmpId, "Gökhan Şahin", "gokhan@omniflow.com", UserRole.Requester, "Account Executive", SalesDeptId, null, Gender.Male, "+90 532 100 1005"),
                Person(OpsEmpId, "Hande Arslan", "hande@omniflow.com", UserRole.Requester, "Operations Coordinator", OpsDeptId, null, Gender.Female, "+90 532 100 1006"));
            await context.SaveChangesAsync(ct);
        }

        // Give the original demo users real departments/titles/managers so the
        // org chart and directory are coherent (idempotent: only if unset).
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Id == SeedConstants.AdminUserId, ct);
        var bob = await context.Users.FirstOrDefaultAsync(u => u.Id == SeedConstants.AgentUserId, ct);
        var charlie = await context.Users.FirstOrDefaultAsync(u => u.Id == SeedConstants.ApproverUserId, ct);
        var alice = await context.Users.FirstOrDefaultAsync(u => u.Id == SeedConstants.RequesterUserId, ct);
        var dirty = false;
        if (admin is not null && admin.DepartmentId != ItDeptId) { admin.DepartmentId = ItDeptId; admin.Title ??= "Platform Administrator"; dirty = true; }
        if (bob is not null && bob.DepartmentId != ItDeptId) { bob.DepartmentId = ItDeptId; bob.Title ??= "IT Support Agent"; bob.ManagerId ??= ItManagerId; dirty = true; }
        if (charlie is not null && charlie.DepartmentId != HrDeptId) { charlie.DepartmentId = HrDeptId; charlie.Title ??= "HR Manager"; dirty = true; }
        if (alice is not null && alice.DepartmentId != SalesDeptId) { alice.DepartmentId = SalesDeptId; alice.Title ??= "Sales Associate"; alice.ManagerId ??= SalesEmpId; dirty = true; }
        if (dirty) await context.SaveChangesAsync(ct);
    }

    private static async Task SeedDirectoryProfilesAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (await context.DirectoryProfiles.AnyAsync(ct)) return;

        DirectoryProfile Profile(Guid userId, string bio, params string[] skills) => new()
        {
            UserId = userId,
            Bio = bio,
            Skills = skills.ToList(),
            ShowPhoneNumber = true,
        };

        context.DirectoryProfiles.AddRange(
            Profile(ItManagerId, "Leads the IT team; owns infrastructure and the service desk.", "ITIL", "Azure", "Networking", "Leadership"),
            Profile(ItAgent2Id, "Systems engineer focused on endpoints and identity.", "Windows", "Intune", "PowerShell", "Active Directory"),
            Profile(HrSpecialistId, "Handles onboarding, payroll queries and benefits.", "Recruiting", "Payroll", "Onboarding"),
            Profile(SeedConstants.AgentUserId, "First-line IT support for hardware and access requests.", "Support", "Hardware", "Troubleshooting"),
            Profile(SeedConstants.ApproverUserId, "HR manager; approves leave and expense requests.", "HR Policy", "Approvals", "People Ops"));
        await context.SaveChangesAsync(ct);
    }

    // ── Asset fleet ─────────────────────────────────────────────────────
    private static async Task SeedAssetsAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (await context.Assets.AnyAsync(ct)) return;

        var owners = new[]
        {
            SeedConstants.AdminUserId, SeedConstants.AgentUserId, SeedConstants.ApproverUserId,
            SeedConstants.RequesterUserId, ItManagerId, ItAgent2Id, HrSpecialistId,
            FinanceEmpId, SalesEmpId, OpsEmpId,
        };
        var models = new[]
        {
            ("Laptop", "MacBook Pro 14"), ("Laptop", "Dell Latitude 7440"), ("Laptop", "Lenovo ThinkPad X1"),
            ("Monitor", "Dell UltraSharp U2723"), ("Monitor", "LG 27UP850"),
            ("Mobile Phone", "iPhone 15"), ("Mobile Phone", "Samsung Galaxy S24"),
            ("Docking Station", "CalDigit TS4"), ("Keyboard", "Keychron K3"), ("Headset", "Jabra Evolve2 65"),
        };

        var assets = new List<Asset>();
        var n = 0;
        // One laptop + one monitor + one phone per employee (assigned).
        foreach (var owner in owners)
        {
            foreach (var (cat, model) in new[] { models[n % 3], models[3 + n % 2], models[5 + n % 2] })
            {
                var a = Asset.Create($"WP-{1000 + n:D4}", $"SN{20240000 + n}", model, cat, AssetStatus.InStock);
                a.AssignTo(owner);
                assets.Add(a);
                n++;
            }
        }
        // A few unassigned / in-repair / retired for a realistic inventory.
        assets.Add(Asset.Create($"WP-{1000 + n++:D4}", $"SN{20240000 + n}", "Dell Latitude 7440", "Laptop", AssetStatus.InStock));
        assets.Add(Asset.Create($"WP-{1000 + n++:D4}", $"SN{20240000 + n}", "LG 27UP850", "Monitor", AssetStatus.InStock));
        var repair = Asset.Create($"WP-{1000 + n++:D4}", $"SN{20240000 + n}", "MacBook Pro 14", "Laptop", AssetStatus.InRepair);
        var retired = Asset.Create($"WP-{1000 + n++:D4}", $"SN{20240000 + n}", "iPhone 13", "Mobile Phone", AssetStatus.Retired);
        assets.Add(repair);
        assets.Add(retired);

        context.Assets.AddRange(assets);
        await context.SaveChangesAsync(ct);
    }

    // ── Tickets across both desks, driven to varied states ──────────────
    private static async Task SeedTicketsAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        // Gate on more than the P0 handful so this replaces/owns ticket seeding.
        if (await context.Tickets.CountAsync(ct) >= 20) return;

        var itTemplate = await context.TicketTemplates
            .FirstOrDefaultAsync(t => t.Id == SeedConstants.ItSupportTemplateId, ct);
        var hrTemplate = await context.TicketTemplates
            .FirstOrDefaultAsync(t => t.Id == HrTemplateId, ct);
        if (itTemplate is null || hrTemplate is null) return;

        var agents = new[] { SeedConstants.AgentUserId, ItAgent2Id, ItManagerId };
        var hrAgents = new[] { SeedConstants.ApproverUserId, HrSpecialistId };
        var requesters = new[] { SeedConstants.RequesterUserId, FinanceEmpId, SalesEmpId, OpsEmpId, ItAgent2Id, HrSpecialistId };

        // (title, description, priority, impact, urgency, category, targetState, assignee, slaHours)
        var itSpecs = new (string T, string D, string P, string I, string U, Guid Cat, TicketState S, Guid A, int Sla)[]
        {
            ("VPN gateway unreachable from the branch office", "Branch office users cannot reach the VPN since 09:00.", "Critical", "High", "High", CatNetworkId, TicketState.InProgress, SeedConstants.AgentUserId, -4),
            ("Laptop won't boot after last update", "Blue screen loop after the Windows feature update.", "High", "High", "High", CatHardwareId, TicketState.InProgress, ItAgent2Id, 6),
            ("Request access to the finance shared drive", "New accountant needs access to the finance share.", "Medium", "Medium", "Medium", CatAccessId, TicketState.New, SeedConstants.AgentUserId, 20),
            ("Outlook keeps asking for password", "Repeated credential prompts on the desktop client.", "Medium", "Medium", "High", CatSoftwareId, TicketState.Resolved, ItAgent2Id, 0),
            ("Monitor flickering at desk 14", "Secondary monitor flickers intermittently.", "Low", "Low", "Low", CatHardwareId, TicketState.Closed, SeedConstants.AgentUserId, 0),
            ("Slow Wi-Fi in the 3rd floor meeting rooms", "Video calls drop in the large meeting rooms.", "Medium", "High", "Medium", CatNetworkId, TicketState.New, ItManagerId, 24),
            ("Install Adobe Creative Cloud", "Design hire needs the Creative Cloud suite.", "Low", "Low", "Low", CatSoftwareId, TicketState.InProgress, ItAgent2Id, 40),
            ("New starter laptop provisioning", "Provision and image a laptop for a new engineer.", "High", "Medium", "High", CatHardwareId, TicketState.InProgress, SeedConstants.AgentUserId, 8),
            ("MFA reset for locked-out user", "User lost their phone and cannot pass MFA.", "High", "High", "High", CatAccessId, TicketState.Resolved, ItManagerId, 0),
            ("Printer on 2nd floor offline", "The shared printer shows offline for everyone.", "Low", "Medium", "Low", CatHardwareId, TicketState.Closed, SeedConstants.AgentUserId, 0),
            ("Email delivery delays to external domains", "Outbound mail delayed 10-15 minutes intermittently.", "High", "High", "Medium", CatNetworkId, TicketState.New, ItManagerId, -2),
            ("Grant GitHub org access", "New engineer needs access to the GitHub organization.", "Medium", "Low", "Medium", CatAccessId, TicketState.Resolved, ItAgent2Id, 0),
            ("Docking station not charging laptop", "USB-C dock stopped delivering power.", "Medium", "Medium", "Medium", CatHardwareId, TicketState.InProgress, SeedConstants.AgentUserId, 12),
            ("VPN split-tunnel configuration", "Route only corporate traffic through the VPN.", "Low", "Low", "Low", CatNetworkId, TicketState.New, ItManagerId, 44),
            ("Shared mailbox for support team", "Create a shared support@ mailbox with delegated access.", "Medium", "Medium", "Medium", CatAccessId, TicketState.InProgress, ItAgent2Id, 16),
            ("License renewal for design tools", "Annual license for the design software is expiring.", "Low", "Medium", "Low", CatSoftwareId, TicketState.Closed, ItManagerId, 0),
            ("Keyboard replacement request", "Several keys unresponsive on the current keyboard.", "Low", "Low", "Low", CatHardwareId, TicketState.Resolved, SeedConstants.AgentUserId, 0),
            ("Onboard new starter to internal wiki", "Provision wiki + SSO access for a new hire.", "Medium", "Low", "Medium", CatAccessId, TicketState.New, ItAgent2Id, 28),
        };

        var hrSpecs = new (string T, string D, string P, string I, string U, Guid Cat, TicketState S, Guid A, int Sla)[]
        {
            ("Payslip discrepancy for last month", "Overtime hours appear to be missing from the payslip.", "High", "High", "Medium", CatPayrollId, TicketState.InProgress, SeedConstants.ApproverUserId, 8),
            ("Onboarding checklist for new engineer", "Prepare the onboarding pack and first-week schedule.", "Medium", "Medium", "Medium", CatOnboardId, TicketState.InProgress, HrSpecialistId, 20),
            ("Question about parental leave policy", "Clarify the paid vs unpaid parental leave split.", "Low", "Low", "Low", CatLeaveQId, TicketState.Resolved, SeedConstants.ApproverUserId, 0),
            ("Update emergency contact details", "Please update my emergency contact on file.", "Low", "Low", "Low", CatBenefitsId, TicketState.Closed, HrSpecialistId, 0),
            ("Health insurance card not received", "New joiner has not received the insurance card.", "Medium", "Medium", "High", CatBenefitsId, TicketState.New, SeedConstants.ApproverUserId, 24),
            ("Request reference letter", "Need an employment reference letter for a visa.", "Low", "Low", "Medium", CatOnboardId, TicketState.Resolved, HrSpecialistId, 0),
            ("Correction to annual leave balance", "My leave balance looks two days short.", "Medium", "Low", "Medium", CatLeaveQId, TicketState.InProgress, SeedConstants.ApproverUserId, 16),
            ("Enroll in the pension scheme", "I'd like to opt in to the company pension.", "Low", "Low", "Low", CatBenefitsId, TicketState.New, HrSpecialistId, 40),
            ("Payroll bank account change", "Please update my salary bank account details.", "High", "Medium", "High", CatPayrollId, TicketState.Resolved, SeedConstants.ApproverUserId, 0),
            ("Onboarding IT accounts for new hire", "Coordinate IT account creation for a new starter.", "Medium", "Medium", "Medium", CatOnboardId, TicketState.Closed, HrSpecialistId, 0),
            ("Sick leave documentation query", "What documentation is needed for extended sick leave?", "Low", "Low", "Low", CatLeaveQId, TicketState.New, SeedConstants.ApproverUserId, 30),
            ("Update tax declaration details", "Need to update my tax declaration for the new year.", "Medium", "Low", "Medium", CatPayrollId, TicketState.InProgress, HrSpecialistId, 18),
        };

        var i = 0;
        foreach (var s in itSpecs)
            await CreateAndDriveTicketAsync(context, itTemplate, requesters[i % requesters.Length], "Incident", s, agents, ct, i++);
        foreach (var s in hrSpecs)
            await CreateAndDriveTicketAsync(context, hrTemplate, requesters[i % requesters.Length], "ServiceRequest", s, hrAgents, ct, i++);
    }

    private static async Task CreateAndDriveTicketAsync(
        OmniFlowDbContext context, TicketTemplate template, Guid requesterId, string ticketType,
        (string T, string D, string P, string I, string U, Guid Cat, TicketState S, Guid A, int Sla) spec,
        Guid[] agentPool, CancellationToken ct, int index)
    {
        var data = new Dictionary<string, object>
        {
            ["Title"] = spec.T,
            ["Description"] = spec.D,
            ["Type"] = ticketType,
            ["Priority"] = spec.P,
            ["Impact"] = spec.I,
            ["Urgency"] = spec.U,
        };
        if (template.Id == HrTemplateId) data["RequestType"] = "Other";

        // Open tickets carry an SLA deadline (negative hours = already breached,
        // for a realistic "breaches" count); resolved/closed ones don't need one.
        TimeSpan? sla = spec.Sla != 0 ? TimeSpan.FromHours(spec.Sla) : null;
        var ticket = Ticket.Create(template, requesterId, data, sla);
        ticket.CategoryId = spec.Cat;
        ticket.ServiceDeskId = template.ServiceDeskId;
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync(ct);

        // Reload with the workflow so the aggregate can drive transitions.
        var t = await context.Tickets.Include(x => x.Template).FirstAsync(x => x.Id == ticket.Id, ct);
        var agent = await context.Users.FirstAsync(u => u.Id == spec.A, ct);
        t.Assign(spec.A, spec.A);

        var triggers = spec.S switch
        {
            TicketState.InProgress => new[] { "StartWork" },
            TicketState.Resolved => new[] { "StartWork", "RequestApproval", "Approve" },
            TicketState.Closed => new[] { "StartWork", "RequestApproval", "Approve", "Close" },
            _ => Array.Empty<string>(),
        };
        foreach (var trigger in triggers)
            t.Transition(trigger, agent, isSystem: true);

        // A comment on roughly every other ticket for conversational realism.
        if (index % 2 == 0)
            t.AddComment(spec.A, "Thanks for raising this — taking a look now and will update shortly.");

        await context.SaveChangesAsync(ct);
    }

    // ── Leave requests across users, types and states ───────────────────
    private static async Task SeedLeaveAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (await context.LeaveRequests.AnyAsync(ct)) return;

        var today = DateTime.UtcNow.Date;
        // (user, type, startOffsetDays, days, state, reason)
        var specs = new (Guid U, LeaveType T, int Start, int Days, LeaveStatus S, string R)[]
        {
            (SeedConstants.RequesterUserId, LeaveType.Annual, 14, 5, LeaveStatus.Approved, "Family holiday"),
            (FinanceEmpId, LeaveType.Sick, -3, 2, LeaveStatus.Approved, "Flu"),
            (SalesEmpId, LeaveType.Annual, 30, 10, LeaveStatus.Pending, "Summer break"),
            (OpsEmpId, LeaveType.Annual, 7, 3, LeaveStatus.Pending, "Long weekend"),
            (ItAgent2Id, LeaveType.Unpaid, 45, 4, LeaveStatus.Pending, "Personal matter"),
            (HrSpecialistId, LeaveType.Annual, -20, 5, LeaveStatus.Approved, "Spring vacation"),
            (SalesEmpId, LeaveType.Sick, -10, 1, LeaveStatus.Approved, "Migraine"),
            (FinanceEmpId, LeaveType.Annual, 60, 7, LeaveStatus.Pending, "Overseas trip"),
            (SeedConstants.AgentUserId, LeaveType.Paternity, 21, 10, LeaveStatus.Approved, "New baby"),
            (OpsEmpId, LeaveType.Annual, -30, 2, LeaveStatus.Rejected, "Understaffed that week"),
            (ItManagerId, LeaveType.Annual, 90, 14, LeaveStatus.Pending, "Year-end leave"),
            (SeedConstants.RequesterUserId, LeaveType.Other, 5, 1, LeaveStatus.Rejected, "Non-standard request"),
        };

        var approver = SeedConstants.ApproverUserId;
        foreach (var s in specs)
        {
            var leave = LeaveRequest.Create(s.U, s.T, today.AddDays(s.Start), today.AddDays(s.Start + s.Days), s.R);
            if (s.S == LeaveStatus.Approved) leave.Approve(approver);
            else if (s.S == LeaveStatus.Rejected) leave.Reject(approver);
            context.LeaveRequests.Add(leave);
        }
        await context.SaveChangesAsync(ct);
    }

    // ── Expense claims across users, categories and states ──────────────
    private static async Task SeedExpensesAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        if (await context.ExpenseClaims.AnyAsync(ct)) return;

        var today = DateTime.UtcNow.Date;
        // (user, title, amount, category, dayOffset, state)
        var specs = new (Guid U, string T, decimal A, ExpenseCategory C, int D, ExpenseStatus S)[]
        {
            (SalesEmpId, "Client dinner — Q3 renewal", 1250m, ExpenseCategory.Meals, -5, ExpenseStatus.Approved),
            (SalesEmpId, "Flight to İzmir for pitch", 3400m, ExpenseCategory.Travel, -12, ExpenseStatus.Paid),
            (ItManagerId, "Conference ticket — DevOps Days", 2800m, ExpenseCategory.Training, -20, ExpenseStatus.Approved),
            (FinanceEmpId, "Accounting software annual", 4200m, ExpenseCategory.Equipment, -8, ExpenseStatus.Pending),
            (OpsEmpId, "Team lunch", 900m, ExpenseCategory.Meals, -2, ExpenseStatus.Pending),
            (ItAgent2Id, "USB-C hubs (x3)", 1500m, ExpenseCategory.Equipment, -15, ExpenseStatus.Approved),
            (HrSpecialistId, "Recruiting event booth", 5600m, ExpenseCategory.Other, -25, ExpenseStatus.Paid),
            (SalesEmpId, "Hotel — client visit", 2100m, ExpenseCategory.Accommodation, -18, ExpenseStatus.Rejected),
            (FinanceEmpId, "Online course — IFRS", 1800m, ExpenseCategory.Training, -30, ExpenseStatus.Approved),
            (SeedConstants.RequesterUserId, "Taxi to airport", 350m, ExpenseCategory.Travel, -1, ExpenseStatus.Pending),
        };

        var approver = SeedConstants.ApproverUserId;
        foreach (var s in specs)
        {
            context.ExpenseClaims.Add(new ExpenseClaim
            {
                Id = Guid.NewGuid(),
                TenantId = SeedConstants.DefaultTenantId,
                UserId = s.U,
                Title = s.T,
                Amount = s.A,
                Currency = "TRY",
                ExpenseDate = DateTime.SpecifyKind(today.AddDays(s.D), DateTimeKind.Utc),
                Category = s.C,
                Status = s.S,
                ApproverId = s.S is ExpenseStatus.Approved or ExpenseStatus.Rejected or ExpenseStatus.Paid ? approver : null,
            });
        }
        await context.SaveChangesAsync(ct);
    }

    // ── Role-specialize the sidebar so IT and HR see their own screens ──
    private static async Task SpecializeMenusAsync(OmniFlowDbContext context, CancellationToken ct)
    {
        var menus = await context.AppMenus.ToListAsync(ct);
        if (menus.Count == 0) return;

        // Map each top-level section (and its children) to the roles that should
        // see it. The frontend hides an item whose RequiredRoles the user lacks;
        // Administrator holds everything, so admins still see all sections.
        (string Section, string Roles)[] sections =
        {
            ("sidebar.itsm", "Administrator,ITAgent"),
            ("sidebar.hr", "Administrator,HRAdmin"),
            ("sidebar.admin", "Administrator"),
        };

        var dirty = false;
        foreach (var (sectionKey, roles) in sections)
        {
            var parent = menus.FirstOrDefault(m => m.TitleKey == sectionKey);
            if (parent is null) continue;
            var targets = menus.Where(m => m.Id == parent.Id || m.ParentId == parent.Id);
            foreach (var menu in targets)
            {
                if (menu.RequiredRoles != roles) { menu.RequiredRoles = roles; dirty = true; }
            }
        }
        if (dirty) await context.SaveChangesAsync(ct);
    }
}
