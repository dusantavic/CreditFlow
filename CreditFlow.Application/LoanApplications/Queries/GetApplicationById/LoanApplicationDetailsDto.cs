using CreditFlow.Application.Common.Models;


namespace CreditFlow.Application.LoanApplications.Queries.GetApplicationById
{
	/// <summary>
	/// Full read-side representation of a loan application. Deliberately flat
	/// and serialization-friendly — no Domain types leak through here, only
	/// primitives and nested DTOs, so the API layer never needs to reference
	/// CreditFlow.Domain directly for read operations.
	/// </summary>
	public sealed record LoanApplicationDetailsDto(
		Guid Id,
		Guid ApplicantId,
		decimal RequestedAmount,
		string Currency,
		int RequestedTermMonths,
		string Purpose,
		string Status,
		DateTime SubmittedAtUtc,
		CreditAssessmentDto? CreditAssessment,
		LoanTermsDto? ApprovedTerms,
		IReadOnlyList<string>? RejectionReasons);

}
