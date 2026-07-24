using System;
using OmniFlow.Application.Employees.Commands;
using OmniFlow.Application.Employees.Queries;
using OmniFlow.Domain.Entities.HR;
using Xunit;

namespace OmniFlow.Application.UnitTests.Employees
{
    public class EmployeeProfileTests
    {
        [Fact]
        public void GermanyDetails_DefaultValues_AreCorrect()
        {
            var de = new GermanyDetails
            {
                TaxId = "12345678901",
                SocialSecurityNo = "12 150789 S 042",
                HealthInsuranceProvider = "Techniker Krankenkasse (TK)",
                TaxClass = 1
            };

            Assert.Equal("12345678901", de.TaxId);
            Assert.Equal("12 150789 S 042", de.SocialSecurityNo);
            Assert.Equal("Techniker Krankenkasse (TK)", de.HealthInsuranceProvider);
            Assert.Equal(1, de.TaxClass);
        }

        [Fact]
        public void TurkeyDetails_DefaultValues_AreCorrect()
        {
            var tr = new TurkeyDetails
            {
                TcKimlikNo = "12345678901",
                SgkNo = "SGK-987654321",
                MilitaryStatus = MilitaryStatus.Done,
                DisabilityDegree = 0
            };

            Assert.Equal("12345678901", tr.TcKimlikNo);
            Assert.Equal("SGK-987654321", tr.SgkNo);
            Assert.Equal(MilitaryStatus.Done, tr.MilitaryStatus);
            Assert.Equal(0, tr.DisabilityDegree);
        }

        [Fact]
        public void UpdateMyHrProfileCommand_Initialization_Works()
        {
            var cmd = new UpdateMyHrProfileCommand
            {
                UserId = Guid.NewGuid(),
                Country = "DE",
                BankName = "Deutsche Bank",
                Iban = "DE89370400440532013000"
            };

            Assert.Equal("DE", cmd.Country);
            Assert.Equal("Deutsche Bank", cmd.BankName);
            Assert.Equal("DE89370400440532013000", cmd.Iban);
        }
    }
}
