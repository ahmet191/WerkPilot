using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class ExpenseClaim : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime ExpenseDate { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? Description { get; set; }
    public string? ReceiptUrl { get; set; }
    
    public ExpenseStatus Status { get; set; }
    public Guid? ApproverId { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public User? Approver { get; set; }
}
