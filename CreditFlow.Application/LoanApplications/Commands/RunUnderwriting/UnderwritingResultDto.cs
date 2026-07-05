

namespace CreditFlow.Application.LoanApplications.Commands.RunUnderwriting
{
	/// <summary>
	/// Full outcome of an underwriting run, returned directly to the caller so
	/// no follow-up query is needed to see WHY a decision was made. Mirrors
	/// UnderwritingDecision/CreditAssessment from the Domain layer, but as a
	/// flat, serialization-friendly shape — the API layer should never need to
	/// reference Domain value objects directly.
	/// </summary>
	public sealed record UnderwritingResultDto(
		bool IsApproved,
		string RiskTier,
		int CreditScore,
		decimal DebtToIncomeRatioPercentage,
		IReadOnlyList<string> Reasons,
		ApprovedLoanTermsDto? ApprovedTerms);

	public sealed record ApprovedLoanTermsDto(
		decimal PrincipalAmount,
		string Currency,
		decimal AnnualInterestRatePercentage,
		int TermMonths,
		decimal MonthlyPayment);
}
