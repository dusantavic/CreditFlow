using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Queries.GetLoanApplications
{
	/// <summary>
	/// Lightweight, list-friendly representation of a loan application —
	/// deliberately excludes full assessment reasons and rejection details
	/// (available via GetLoanApplicationByIdQuery) to keep list responses small.
	/// </summary>
	public sealed record LoanApplicationSummaryDto(
		Guid Id,
		Guid ApplicantId,
		decimal RequestedAmount,
		string Currency,
		string Status,
		string? RiskTier,
		DateTime SubmittedAtUtc); 
}
