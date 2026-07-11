using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Persistence.Interceptors;
using System.Reflection;

namespace OmniFlow.Infrastructure.Persistence;

public class OmniFlowDbContext : DbContext, IOmniFlowDbContext
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

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<EmploymentDetails> EmploymentDetails => Set<EmploymentDetails>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<Asset> Assets => Set<Asset>();
    
    public DbSet<Compensation> Compensations => Set<Compensation>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
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

        base.OnModelCreating(builder);
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
