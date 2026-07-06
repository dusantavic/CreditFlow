
using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.ApproveLoanApplication
{
	/// <summary>
	/// Manually approves a loan application with specific terms, bypassing
	/// (or overriding) the automated UnderwritingPolicy outcome. Used when a
	/// human underwriter makes the final call — e.g. approving despite an
	/// automated decline, or adjusting the proposed terms.
	/// </summary>
	public sealed record ApproveLoanApplicationCommand(
		Guid LoanApplicationId,
		decimal ApprovedAmount,
		string Currency,
		decimal AnnualInterestRatePercentage,
		int TermMonths) : IRequest; 
}
