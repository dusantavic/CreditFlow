

using CreditFlow.Application.Common.Models;

namespace CreditFlow.Application.LoanApplications.Commands.RunUnderwriting
{
	/// <summary>
	/// Full outcome of an underwriting run, returned directly to the caller so
	/// no follow-up query is needed to see WHY a decision was made.
	/// </summary>
	public sealed record UnderwritingResultDto(
		bool IsApproved,
		string RiskTier,
		int CreditScore,
		decimal DebtToIncomeRatioPercentage,
		IReadOnlyList<string> Reasons,
		LoanTermsDto? ApprovedTerms);
}
