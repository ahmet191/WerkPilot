using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Application.Employees.Commands
{
    public class UpdateMyHrProfileCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public string Country { get; set; } = "TR";
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }

        public string BankName { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string? BicSwift { get; set; }

        public GermanyDetails? GermanyDetails { get; set; }
        public TurkeyDetails? TurkeyDetails { get; set; }
    }

    public class UpdateMyHrProfileCommandHandler : IRequestHandler<UpdateMyHrProfileCommand, bool>
    {
        private readonly IOmniFlowDbContext _context;

        public UpdateMyHrProfileCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateMyHrProfileCommand request, CancellationToken cancellationToken)
        {
            var profile = await _context.EmployeeProfiles
                .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

            if (profile == null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User '{request.UserId}' not found.");
                }

                profile = new EmployeeProfile
                {
                    UserId = request.UserId,
                    TenantId = user.TenantId,
                    HireDate = DateTime.UtcNow
                };
                _context.EmployeeProfiles.Add(profile);
            }

            if (Enum.TryParse<CountryCode>(request.Country, true, out var countryCode))
            {
                profile.Country = countryCode;
            }

            profile.BirthDate = request.BirthDate;
            profile.Gender = request.Gender;
            profile.BloodType = request.BloodType;
            profile.EmergencyContactName = request.EmergencyContactName;
            profile.EmergencyContactPhone = request.EmergencyContactPhone;
            profile.AddressLine = request.AddressLine;
            profile.City = request.City;
            profile.PostalCode = request.PostalCode;

            profile.BankName = request.BankName;
            profile.Iban = request.Iban;
            profile.BicSwift = request.BicSwift;

            if (request.GermanyDetails != null)
            {
                profile.GermanyDetails = request.GermanyDetails;
            }
            if (request.TurkeyDetails != null)
            {
                profile.TurkeyDetails = request.TurkeyDetails;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
