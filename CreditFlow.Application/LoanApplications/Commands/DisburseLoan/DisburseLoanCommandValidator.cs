using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Commands.DisburseLoan
{
	public sealed class DisburseLoanCommandValidator : AbstractValidator<DisburseLoanCommand>
	{
		public DisburseLoanCommandValidator()
		{
			RuleFor(x => x.LoanApplicationId).NotEmpty();
		}
	}
}
