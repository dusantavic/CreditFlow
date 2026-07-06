

namespace CreditFlow.Application.Common.Models
{
	/// <summary>
	/// Flat, serialization-friendly representation of LoanTerms. Shared across
	/// every use case that needs to expose approved loan terms (RunUnderwriting,
	/// GetLoanApplicationById, and any future query/command), so this mapping
	/// exists in exactly one place.
	/// </summary>
	public sealed record LoanTermsDto(
		decimal PrincipalAmount,
		string Currency,
		decimal AnnualInterestRatePercentage,
		int TermMonths,
		decimal MonthlyPayment);
}
