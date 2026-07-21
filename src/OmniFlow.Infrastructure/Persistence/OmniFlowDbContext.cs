using MassTransit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Identity;
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
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
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

    public DbSet<Domain.Entities.Approvals.ApprovalRequest> ApprovalRequests => Set<Domain.Entities.Approvals.ApprovalRequest>();
    public DbSet<Domain.Entities.Approvals.ApprovalStep> ApprovalSteps => Set<Domain.Entities.Approvals.ApprovalStep>();

    // Pre-auth SSO exchange tickets: infrastructure-only, outside the tenant filter.
    public DbSet<Identity.SsoTicket> SsoTickets => Set<Identity.SsoTicket>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<TranslationOverride> TranslationOverrides => Set<TranslationOverride>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must call base first to configure Identity tables
        base.OnModelCreating(builder);

        // Configure OpenIddict tables with Guid keys
        builder.UseOpenIddict<Guid>();

        // MassTransit transactional outbox tables (OutboxMessage/OutboxState/InboxState)
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<TicketAttachment>(entity =>
        {
            entity.Property(a => a.FileName).HasMaxLength(260).IsRequired();
            entity.Property(a => a.ContentType).HasMaxLength(128);
            entity.Property(a => a.StorageKey).HasMaxLength(200).IsRequired();
            entity.HasIndex(a => a.TicketId);
            entity.HasOne(a => a.UploadedBy).WithMany().HasForeignKey(a => a.UploadedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Ticket>().WithMany(t => t.Attachments).HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TranslationOverride>(entity =>
        {
            entity.Property(t => t.Language).HasMaxLength(8).IsRequired();
            entity.Property(t => t.Key).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Value).HasMaxLength(2000).IsRequired();
            entity.HasIndex(t => new { t.TenantId, t.Language, t.Key }).IsUnique();
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Body).HasMaxLength(2000);
            entity.Property(n => n.Link).HasMaxLength(500);
            // The centre reads "my unread, newest first" on every page load.
            entity.HasIndex(n => new { n.TenantId, n.RecipientUserId, n.ReadAt });
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.Property(p => p.RoleName).HasMaxLength(64).IsRequired();
            entity.Property(p => p.Permission).HasMaxLength(128).IsRequired();
            entity.HasIndex(p => new { p.TenantId, p.RoleName, p.Permission }).IsUnique();
        });

        builder.Entity<Identity.SsoTicket>(entity =>
        {
            entity.ToTable("SsoTickets");
            entity.Property(t => t.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(t => t.CodeHash).IsUnique();
            entity.Property(t => t.Provider).HasMaxLength(64);
        });

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
        // Single combined filter: EF allows only ONE HasQueryFilter per entity —
        // a second call would silently replace the first.
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId && !e.IsDeleted);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
    }
}
