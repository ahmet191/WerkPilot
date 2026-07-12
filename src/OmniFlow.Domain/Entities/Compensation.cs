using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class Compensation : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public string Currency { get; set; } = "TRY";
    public string PayFrequency { get; set; } = "Monthly";
    public string? BankAccountNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    
    // Navigation
    public User Employee { get; set; } = null!;
}
