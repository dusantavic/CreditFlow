using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.LoanApplications;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Application.UnitTests.TestHelpers
{
	/// <summary>
	/// Builders for real domain objects used as handler inputs/outputs. Domain
	/// types have private constructors and factory validation, so tests build
	/// them through the same public factories production code uses.
	/// </summary>
	internal static class DomainFixtures
	{
		public static Applicant CreateApplicant(decimal monthlyIncome = 5_000m, string currency = "EUR")
			=> Applicant.Register(
				PersonalInfo.Of("John", "Doe", new DateOnly(1990, 5, 15), "1234567890"),
				EmploymentInfo.Of("Acme Corp", Money.Of(monthlyIncome, currency), 5),
				FinancialObligations.None(currency));

		public static LoanApplication CreateLoanApplication(
			LoanApplicationStatus status = LoanApplicationStatus.Draft,
			Guid? applicantId = null,
			decimal requestedAmount = 10_000m,
			int termMonths = 12)
		{
			var application = LoanApplication.CreateDraft(
				applicantId ?? Guid.NewGuid(),
				Money.Of(requestedAmount, "EUR"),
				termMonths,
				"Car purchase");

			if (status is LoanApplicationStatus.Submitted
				or LoanApplicationStatus.UnderReview
				or LoanApplicationStatus.Approved
				or LoanApplicationStatus.Rejected
				or LoanApplicationStatus.Disbursed)
			{
				application.Submit();
			}

			if (status is LoanApplicationStatus.UnderReview
				or LoanApplicationStatus.Approved
				or LoanApplicationStatus.Rejected
				or LoanApplicationStatus.Disbursed)
			{
				application.StartReview();
			}

			if (status is LoanApplicationStatus.Approved or LoanApplicationStatus.Disbursed)
				application.Approve(SampleTerms(requestedAmount, termMonths));

			if (status is LoanApplicationStatus.Rejected)
				application.Reject(new[] { "rejected in test setup" });

			if (status is LoanApplicationStatus.Disbursed)
				application.Disburse();

			if (status is LoanApplicationStatus.Cancelled)
				application.Cancel();

			application.ClearDomainEvents();
			return application;
		}

		public static LoanTerms SampleTerms(decimal principal = 10_000m, int termMonths = 12)
			=> LoanTerms.Of(Money.Of(principal, "EUR"), InterestRate.OfAnnual(5.5m), termMonths);

		public static CreditAssessment SampleAssessment(
			int score = 780,
			RiskTier tier = RiskTier.Prime,
			decimal dti = 20m,
			IReadOnlyList<string>? reasons = null)
			=> CreditAssessment.Of(CreditScore.Of(score), tier, Percentage.Of(dti), reasons ?? new[] { "reason" });

		public static UnderwritingDecision ApprovedDecision(
			LoanTerms? terms = null, IReadOnlyList<string>? reasons = null)
		{
			reasons ??= new[] { "Approved at risk tier Prime with DTI 20.00%." };
			return UnderwritingDecision.Approve(terms ?? SampleTerms(), SampleAssessment(reasons: reasons), reasons);
		}

		public static UnderwritingDecision DeclinedDecision(IReadOnlyList<string>? reasons = null)
		{
			reasons ??= new[] { "Debt-to-income ratio of 86.00% exceeds the maximum allowed 45%." };
			return UnderwritingDecision.Decline(
				SampleAssessment(score: 780, tier: RiskTier.Prime, dti: 86m, reasons: reasons), reasons);
		}
	}
}
