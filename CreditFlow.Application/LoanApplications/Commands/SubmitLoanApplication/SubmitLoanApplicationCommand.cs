using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.SubmitLoanApplication
{
	/// <summary>
	/// Submits a new loan application on behalf of an existing applicant.
	/// Creates the LoanApplication as a Draft and immediately transitions it
	/// to Submitted in the same operation — there's currently no use case for
	/// leaving an application sitting in Draft, so this command collapses both
	/// steps rather than exposing a separate "create draft" endpoint.
	/// </summary>
	public sealed record SubmitLoanApplicationCommand(
		Guid ApplicantId,
		decimal RequestedAmount,
		string Currency,
		int RequestedTermMonths,
		string Purpose) : IRequest<Guid>;
}
