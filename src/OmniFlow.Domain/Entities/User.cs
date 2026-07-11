using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class User : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    
    // Core HR Details
    public string? Title { get; set; }
    public string? IdentificationNumber { get; set; } // TCKN or Tax ID
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.PreferNotToSay;
    public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Single;
    public string? Nationality { get; set; }
    
    // Contact Info
    public string? PersonalEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmergencyContact { get; set; }

    public Guid? ManagerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? TeamId { get; set; }
    
    // Preferences
    public string LanguagePreference { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public string? TelegramChatId { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    // Navigation
    public User? Manager { get; set; }
    public Department? Department { get; set; }
    public Location? Location { get; set; }
    public Team? Team { get; set; }
}
