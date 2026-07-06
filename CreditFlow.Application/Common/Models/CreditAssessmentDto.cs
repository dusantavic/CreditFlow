
namespace CreditFlow.Application.Common.Models
{
	/// <summary>
	/// Flat, serialization-friendly representation of CreditAssessment. Shared
	/// across every use case that needs to expose an assessment's outcome.
	/// </summary>
	public sealed record CreditAssessmentDto(
		int CreditScore,
		string RiskTier,
		decimal DebtToIncomeRatioPercentage,
		IReadOnlyList<string> Reasons,
		DateTime AssessedAtUtc);
}
