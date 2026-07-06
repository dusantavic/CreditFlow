using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Commands.ApproveLoanApplication
{
	public sealed class ApproveLoanApplicationCommandValidator : AbstractValidator<ApproveLoanApplicationCommand>
	{
		public ApproveLoanApplicationCommandValidator()
		{
			RuleFor(x => x.LoanApplicationId).NotEmpty();
			RuleFor(x => x.ApprovedAmount).GreaterThan(0);

			RuleFor(x => x.Currency)
				.NotEmpty()
				.Length(3)
				.WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. USD, EUR).");

			RuleFor(x => x.AnnualInterestRatePercentage).InclusiveBetween(0, 100);
			RuleFor(x => x.TermMonths).GreaterThan(0).LessThanOrEqualTo(360);
		}
	}
}
