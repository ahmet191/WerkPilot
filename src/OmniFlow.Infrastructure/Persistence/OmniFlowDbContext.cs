using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Persistence.Interceptors;
using System.Reflection;
using HR = OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Infrastructure.Persistence;

public class OmniFlowDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IOmniFlowDbContext
{
    private readonly AuditableEntityInterceptor _auditableEntityInterceptor;
    private readonly ITenantService _tenantService;

    public OmniFlowDbContext(
        DbContextOptions<OmniFlowDbContext> options,
        AuditableEntityInterceptor auditableEntityInterceptor,
        ITenantService tenantService) 
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
        _tenantService = tenantService;
    }

    public new DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<EmploymentDetails> EmploymentDetails => Set<EmploymentDetails>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<Asset> Assets => Set<Asset>();
    
    public DbSet<OmniFlow.Domain.Entities.Compensation> Compensations => Set<OmniFlow.Domain.Entities.Compensation>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    // HR entities (fully qualified to avoid ambiguity with HR namespace)
    public DbSet<HR.Employee> Employees => Set<HR.Employee>();
    public DbSet<HR.Position> Positions => Set<HR.Position>();
    public DbSet<HR.AttendanceRecord> AttendanceRecords => Set<HR.AttendanceRecord>();
    public DbSet<HR.TimeOffRequest> TimeOffRequests => Set<HR.TimeOffRequest>();
    public DbSet<HR.PayrollRun> PayrollRuns => Set<HR.PayrollRun>();
    public DbSet<HR.Benefit> Benefits => Set<HR.Benefit>();
    
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketTemplate> TicketTemplates => Set<TicketTemplate>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAuditLog> TicketAuditLogs => Set<TicketAuditLog>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    
    public DbSet<ConfigurationItem> ConfigurationItems => Set<ConfigurationItem>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    
    public DbSet<ServiceDesk> ServiceDesks => Set<ServiceDesk>();
    public DbSet<ExpenseClaim> ExpenseClaims { get; set; }
    public DbSet<AuthorityDelegation> AuthorityDelegations { get; set; }
    public DbSet<DirectoryProfile> DirectoryProfiles { get; set; }
    
    public DbSet<AppMenu> AppMenus { get; set; }

    public DbSet<SLAConfiguration> SLAConfigurations { get; set; }
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles => Set<KnowledgeBaseArticle>();
    public DbSet<AuditLog> AuditLogs { get; set; }

    // Tenant registry: deliberately NOT an AuditableEntity, so it stays outside
    // the tenant global query filter (it is the root the filter resolves against).
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must call base first to configure Identity tables
        base.OnModelCreating(builder);

        // Configure OpenIddict tables with Guid keys
        builder.UseOpenIddict<Guid>();

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global Query Filter for Multi-Tenancy
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(OmniFlow.Domain.Common.AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(OmniFlowDbContext)
                    .GetMethod(nameof(SetGlobalQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);
                
                method?.Invoke(this, new object[] { builder });
            }
        }
    }

    private void SetGlobalQueryFilter<TEntity>(ModelBuilder builder) where TEntity : OmniFlow.Domain.Common.AuditableEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
    }
}
