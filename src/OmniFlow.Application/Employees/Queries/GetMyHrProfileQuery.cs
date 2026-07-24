using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Application.Employees.Queries
{
    public class EmployeeAssetSummaryDto
    {
        public Guid Id { get; set; }
        public string AssetTag { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class EmployeeLeaveBalanceSummaryDto
    {
        public string LeaveType { get; set; } = string.Empty;
        public decimal TotalEntitlementDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal RemainingDays => TotalEntitlementDays - UsedDays;
    }

    public class HrProfileDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        
        public string Country { get; set; } = "TR"; // "DE" or "TR"
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }

        public DateTime HireDate { get; set; }
        public DateTime? TrialPeriodEndDate { get; set; }
        public string EmploymentType { get; set; } = "FullTime";
        public decimal WorkingHoursPerWeek { get; set; } = 40.0m;
        public string? ManagerName { get; set; }

        public decimal GrossSalary { get; set; }
        public string Currency { get; set; } = "TRY";
        public string BankName { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string? BicSwift { get; set; }

        // Country details
        public GermanyDetails GermanyDetails { get; set; } = new GermanyDetails();
        public TurkeyDetails TurkeyDetails { get; set; } = new TurkeyDetails();

        public List<EmployeeAssetSummaryDto> AssignedAssets { get; set; } = new();
        public List<EmployeeLeaveBalanceSummaryDto> LeaveBalances { get; set; } = new();
    }

    public record GetMyHrProfileQuery(Guid UserId) : IRequest<HrProfileDto>;

    public class GetMyHrProfileQueryHandler : IRequestHandler<GetMyHrProfileQuery, HrProfileDto>
    {
        private readonly IOmniFlowDbContext _context;

        public GetMyHrProfileQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<HrProfileDto> Handle(GetMyHrProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.UserId}' was not found.");
            }

            var profile = await _context.EmployeeProfiles
                .AsNoTracking()
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

            // Assets assigned to this user
            var assets = await _context.Assets
                .AsNoTracking()
                .Where(a => a.AssignedToUserId == request.UserId)
                .Select(a => new EmployeeAssetSummaryDto
                {
                    Id = a.Id,
                    AssetTag = a.AssetTag,
                    Model = a.Model,
                    Category = a.Category,
                    Status = a.Status.ToString()
                })
                .ToListAsync(cancellationToken);

            // Leave balances for this user
            var balances = await _context.LeaveBalances
                .AsNoTracking()
                .Where(b => b.UserId == request.UserId)
                .Select(b => new EmployeeLeaveBalanceSummaryDto
                {
                    LeaveType = b.Type.ToString(),
                    TotalEntitlementDays = b.TotalDaysAllocated,
                    UsedDays = b.TotalDaysUsed
                })
                .ToListAsync(cancellationToken);

            var deptName = "General";
            if (user.Department?.LocalizedName?.Translations != null && user.Department.LocalizedName.Translations.TryGetValue("en", out var dName))
            {
                deptName = dName;
            }

            var dto = new HrProfileDto
            {
                UserId = user.Id,
                FullName = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                DepartmentName = deptName,
                JobTitle = user.Title ?? "Specialist",
                AssignedAssets = assets,
                LeaveBalances = balances
            };

            if (profile != null)
            {
                dto.Country = profile.Country.ToString();
                dto.BirthDate = profile.BirthDate;
                dto.Gender = profile.Gender;
                dto.BloodType = profile.BloodType;
                dto.EmergencyContactName = profile.EmergencyContactName;
                dto.EmergencyContactPhone = profile.EmergencyContactPhone;
                dto.AddressLine = profile.AddressLine;
                dto.City = profile.City;
                dto.PostalCode = profile.PostalCode;
                dto.HireDate = profile.HireDate;
                dto.TrialPeriodEndDate = profile.TrialPeriodEndDate;
                dto.EmploymentType = profile.EmploymentType.ToString();
                dto.WorkingHoursPerWeek = profile.WorkingHoursPerWeek;
                dto.ManagerName = profile.Manager?.Name;
                dto.GrossSalary = profile.GrossSalary;
                dto.Currency = profile.Currency;
                dto.BankName = profile.BankName;
                dto.Iban = profile.Iban;
                dto.BicSwift = profile.BicSwift;
                dto.GermanyDetails = profile.GermanyDetails ?? new GermanyDetails();
                dto.TurkeyDetails = profile.TurkeyDetails ?? new TurkeyDetails();
            }
            else
            {
                // Fallback default profile values
                dto.Country = "TR";
                dto.HireDate = DateTime.UtcNow.AddYears(-1);
                dto.TrialPeriodEndDate = DateTime.UtcNow.AddMonths(-6);
                dto.GrossSalary = 65000;
                dto.Currency = "TRY";
                dto.BankName = "Garanti BBVA";
                dto.Iban = "TR320006200000000000000001";
                dto.TurkeyDetails = new TurkeyDetails { TcKimlikNo = "12345678901", SgkNo = "SGK-987654321" };
            }

            return dto;
        }
    }
}
