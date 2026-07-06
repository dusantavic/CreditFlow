using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Commands.DisburseLoan
{
	/// <summary>
	/// Marks an approved loan as disbursed — the final, irreversible step
	/// where funds are considered transferred to the applicant.
	/// </summary>
	public sealed record DisburseLoanCommand(Guid LoanApplicationId) : IRequest;
}
