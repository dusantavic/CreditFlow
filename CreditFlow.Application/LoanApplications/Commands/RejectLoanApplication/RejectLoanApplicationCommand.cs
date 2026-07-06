using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Commands.RejectLoanApplication
{
	/// <summary>
	/// Manually rejects a loan application (e.g. an underwriter overriding an
	/// automated approval, or rejecting for a reason the policy doesn't
	/// capture, such as suspected fraud).
	/// </summary>
	public sealed record RejectLoanApplicationCommand(
		Guid LoanApplicationId,
		IReadOnlyList<string> Reasons) : IRequest; 
}
