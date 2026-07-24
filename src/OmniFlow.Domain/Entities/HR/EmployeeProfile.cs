using System;
using System.Collections.Generic;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public enum CountryCode
    {
        TR = 1,
        DE = 2
    }

    public enum EmploymentType
    {
        FullTime = 1,
        PartTime = 2,
        Contractor = 3,
        Intern = 4
    }

    public enum MilitaryStatus
    {
        Done = 1,
        Deferred = 2,
        Exempt = 3,
        NotApplicable = 4
    }

    public class GermanyDetails
    {
        public string TaxId { get; set; } = string.Empty; // Steuer-ID (11 digits)
        public string SocialSecurityNo { get; set; } = string.Empty; // SV-Nummer
        public string HealthInsuranceProvider { get; set; } = string.Empty; // Krankenkasse (AOK, TK, etc.)
        public int TaxClass { get; set; } = 1; // Steuerklasse (1-6)
    }

    public class TurkeyDetails
    {
        public string TcKimlikNo { get; set; } = string.Empty; // 11 digit TR ID
        public string SgkNo { get; set; } = string.Empty; // SGK Sicil No
        public MilitaryStatus MilitaryStatus { get; set; } = MilitaryStatus.NotApplicable;
        public int DisabilityDegree { get; set; } = 0; // 0 = None, 1-3 = Degree
    }

    public class EmployeeProfile : AuditableEntity
    {
        public Guid UserId { get; set; }
        public CountryCode Country { get; set; } = CountryCode.TR;
        
        // Personal Details
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        
        // Employment & Probation Details
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public DateTime? TrialPeriodEndDate { get; set; } // Probezeit / Deneme Süresi
        public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
        public decimal WorkingHoursPerWeek { get; set; } = 40.0m;
        public Guid? ManagerId { get; set; }

        // Financial & Compensation (Özlük Hakları)
        public decimal GrossSalary { get; set; }
        public string Currency { get; set; } = "TRY"; // "EUR" or "TRY"
        public string BankName { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string? BicSwift { get; set; }

        // Country-specific details
        public GermanyDetails GermanyDetails { get; set; } = new GermanyDetails();
        public TurkeyDetails TurkeyDetails { get; set; } = new TurkeyDetails();

        // Navigation
        public User? User { get; set; }
        public User? Manager { get; set; }
        public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    }
}
