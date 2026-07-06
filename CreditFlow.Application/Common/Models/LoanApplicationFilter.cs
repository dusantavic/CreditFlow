
using CreditFlow.Domain.LoanApplications;

namespace CreditFlow.Application.Common.Models
{
	/// <summary>
	/// Optional filter criteria for listing loan applications. Every property
	/// is nullable — null means "don't filter on this dimension". Kept as a
	/// plain parameter object rather than individual method parameters so the
	/// repository method signature stays stable as filter criteria grow.
	/// </summary>
	public sealed record LoanApplicationFilter(
		LoanApplicationStatus? Status,
		Guid? ApplicantId,
		DateTime? SubmittedFromUtc,
		DateTime? SubmittedToUtc); 
}
