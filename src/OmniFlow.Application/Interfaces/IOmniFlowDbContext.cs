using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Entities.Approvals;

namespace OmniFlow.Application.Interfaces;

public interface IOmniFlowDbContext
{
    DbSet<User> Users { get; }
    DbSet<Department> Departments { get; }
    DbSet<Location> Locations { get; }
    DbSet<Team> Teams { get; }
    DbSet<EmploymentDetails> EmploymentDetails { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<Asset> Assets { get; }
    
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketTemplate> TicketTemplates { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAuditLog> TicketAuditLogs { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    
    DbSet<ConfigurationItem> ConfigurationItems { get; }
    DbSet<Problem> Problems { get; }
    DbSet<ChangeRequest> ChangeRequests { get; }
    
    DbSet<ServiceDesk> ServiceDesks { get; }
    DbSet<ExpenseClaim> ExpenseClaims { get; }
    DbSet<AuthorityDelegation> AuthorityDelegations { get; }
    DbSet<DirectoryProfile> DirectoryProfiles { get; }
    
    DbSet<AppMenu> AppMenus { get; }
    
    DbSet<SLAConfiguration> SLAConfigurations { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; }
    DbSet<AuditLog> AuditLogs { get; set; }

    DbSet<Tenant> Tenants { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }
    
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
