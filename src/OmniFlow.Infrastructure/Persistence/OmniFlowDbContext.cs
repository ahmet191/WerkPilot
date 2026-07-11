using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Persistence.Interceptors;
using System.Reflection;

namespace OmniFlow.Infrastructure.Persistence;

public class OmniFlowDbContext : DbContext, IOmniFlowDbContext
{
    private readonly AuditableEntityInterceptor _auditableEntityInterceptor;

    public OmniFlowDbContext(
        DbContextOptions<OmniFlowDbContext> options,
        AuditableEntityInterceptor auditableEntityInterceptor) 
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
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
    
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketTemplate> TicketTemplates => Set<TicketTemplate>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAuditLog> TicketAuditLogs => Set<TicketAuditLog>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<SLAConfiguration> SLAConfigurations => Set<SLAConfiguration>();
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles => Set<KnowledgeBaseArticle>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
    }
}
