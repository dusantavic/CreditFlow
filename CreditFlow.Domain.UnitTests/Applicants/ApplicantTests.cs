using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.Applicants
{
	public class ApplicantTests
	{
		private static PersonalInfo ValidPersonalInfo()
			=> PersonalInfo.Of("John", "Doe", new DateOnly(1990, 5, 15), "1234567890");

		private static Applicant RegisterApplicant(
			decimal monthlyIncome = 4_000m,
			decimal existingMonthlyDebt = 500m,
			string currency = "EUR")
			=> Applicant.Register(
				ValidPersonalInfo(),
				EmploymentInfo.Of("Acme Corp", Money.Of(monthlyIncome, currency), 5),
				FinancialObligations.Of(Money.Of(existingMonthlyDebt, currency)));

		[Fact]
		public void Register_WithMatchingCurrencies_CreatesApplicant()
		{
			var personalInfo = ValidPersonalInfo();
			var employmentInfo = EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), 5);
			var financialObligations = FinancialObligations.Of(Money.Of(500m, "EUR"));

			var applicant = Applicant.Register(personalInfo, employmentInfo, financialObligations);

			applicant.Id.Should().NotBeEmpty();
			applicant.PersonalInfo.Should().Be(personalInfo);
			applicant.EmploymentInfo.Should().Be(employmentInfo);
			applicant.FinancialObligations.Should().Be(financialObligations);
			applicant.RegisteredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
		}

		[Fact]
		public void Register_WithMismatchedIncomeAndDebtCurrencies_ThrowsBusinessRuleViolationException()
		{
			var act = () => Applicant.Register(
				ValidPersonalInfo(),
				EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), 5),
				FinancialObligations.Of(Money.Of(500m, "USD")));

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("Monthly income and existing debt must be expressed in the same currency (EUR vs USD).");
		}

		[Fact]
		public void UpdateEmploymentInfo_WithMatchingCurrency_ReplacesEmploymentInfo()
		{
			var applicant = RegisterApplicant();
			var newEmployment = EmploymentInfo.Of("New Employer", Money.Of(5_000m, "EUR"), 0);

			applicant.UpdateEmploymentInfo(newEmployment);

			applicant.EmploymentInfo.Should().Be(newEmployment);
		}

		[Fact]
		public void UpdateEmploymentInfo_WithMismatchedCurrency_ThrowsBusinessRuleViolationException()
		{
			var applicant = RegisterApplicant(currency: "EUR");
			var newEmployment = EmploymentInfo.Of("New Employer", Money.Of(5_000m, "USD"), 0);

			var act = () => applicant.UpdateEmploymentInfo(newEmployment);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("Monthly income and existing debt must be expressed in the same currency (USD vs EUR).");
		}

		[Fact]
		public void UpdateFinancialObligations_WithMatchingCurrency_ReplacesFinancialObligations()
		{
			var applicant = RegisterApplicant();
			var newObligations = FinancialObligations.Of(Money.Of(750m, "EUR"));

			applicant.UpdateFinancialObligations(newObligations);

			applicant.FinancialObligations.Should().Be(newObligations);
		}

		[Fact]
		public void UpdateFinancialObligations_WithMismatchedCurrency_ThrowsBusinessRuleViolationException()
		{
			var applicant = RegisterApplicant(currency: "EUR");
			var newObligations = FinancialObligations.Of(Money.Of(750m, "USD"));

			var act = () => applicant.UpdateFinancialObligations(newObligations);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("Monthly income and existing debt must be expressed in the same currency (EUR vs USD).");
		}

		[Fact]
		public void CalculateDebtToIncomeRatio_WithMidRangeInputs_ReturnsHandCalculatedPercentage()
		{
			// (500 existing + 700 proposed) / 4000 income = 0.30 => 30%
			var applicant = RegisterApplicant(monthlyIncome: 4_000m, existingMonthlyDebt: 500m);

			var ratio = applicant.CalculateDebtToIncomeRatio(Money.Of(700m, "EUR"));

			ratio.Should().Be(Percentage.Of(30m));
		}

		[Fact]
		public void CalculateDebtToIncomeRatio_WhenTotalDebtExceedsIncome_CapsAtOneHundredPercent()
		{
			// (800 existing + 400 proposed) / 1000 income = 120% -> capped to 100%
			var applicant = RegisterApplicant(monthlyIncome: 1_000m, existingMonthlyDebt: 800m);

			var ratio = applicant.CalculateDebtToIncomeRatio(Money.Of(400m, "EUR"));

			ratio.Should().Be(Percentage.Of(100m));
		}

		[Fact]
		public void CalculateDebtToIncomeRatio_WithZeroExistingDebt_IsDrivenOnlyByProposedPayment()
		{
			// (0 existing + 1000 proposed) / 4000 income = 25%
			var applicant = Applicant.Register(
				ValidPersonalInfo(),
				EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), 5),
				FinancialObligations.None("EUR"));

			var ratio = applicant.CalculateDebtToIncomeRatio(Money.Of(1_000m, "EUR"));

			ratio.Should().Be(Percentage.Of(25m));
		}

		[Fact]
		public void CalculateDebtToIncomeRatio_WithMismatchedPaymentCurrency_ThrowsBusinessRuleViolationException()
		{
			var applicant = RegisterApplicant(currency: "EUR");

			var act = () => applicant.CalculateDebtToIncomeRatio(Money.Of(700m, "USD"));

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("Proposed monthly payment must be in the same currency as the applicant's income (EUR vs USD).");
		}
	}
}
