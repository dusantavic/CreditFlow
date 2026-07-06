using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.CancelLoanApplication
{
	/// <summary>
	/// Cancels a loan application on the applicant's own request. Only valid
	/// while the application is still in Draft or Submitted status — see
	/// LoanApplication.Cancel() for the exact rule.
	/// </summary>
	public sealed record CancelLoanApplicationCommand(Guid LoanApplicationId) : IRequest;
}
