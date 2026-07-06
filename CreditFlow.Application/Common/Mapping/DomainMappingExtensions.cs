

using CreditFlow.Application.Common.Models;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Application.Common.Mapping
{
	/// <summary>
	/// Shared mapping helpers from Domain value objects to Application DTOs.
	/// Centralized here so every handler that needs to expose LoanTerms or
	/// CreditAssessment over the API maps them identically, in one place.
	/// </summary>
	public static class DomainMappingExtensions
	{
		public static LoanTermsDto ToDto(this LoanTerms terms)
			=> new(
				terms.PrincipalAmount.Amount,
				terms.PrincipalAmount.Currency,
				terms.InterestRate.AnnualRate.Value,
				terms.TermMonths,
				terms.MonthlyPayment.Amount);

		public static CreditAssessmentDto ToDto(this CreditAssessment assessment)
			=> new(
				assessment.CreditScore.Value,
				assessment.RiskTier.ToString(),
				assessment.DebtToIncomeRatio.Value,
				assessment.Reasons,
				assessment.AssessedAtUtc);
	}
}
