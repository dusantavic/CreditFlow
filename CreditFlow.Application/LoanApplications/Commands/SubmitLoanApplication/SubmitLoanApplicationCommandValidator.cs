using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Commands.SubmitLoanApplication
{
	public sealed class SubmitLoanApplicationCommandValidator : AbstractValidator<SubmitLoanApplicationCommand>
	{
		public SubmitLoanApplicationCommandValidator()
		{
			RuleFor(x => x.ApplicantId).NotEmpty();

			RuleFor(x => x.RequestedAmount).GreaterThan(0); 

			RuleFor(x => x.Currency)
				.NotEmpty()
				.Length(3)
				.WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. USD, EUR).");
		
			RuleFor(x => x.RequestedTermMonths)
				.GreaterThan(0)
				.LessThanOrEqualTo(360)
				.WithMessage("Requested term must be between 1 and 360 months.");

			RuleFor(x => x.Purpose).NotEmpty().MaximumLength(500);
		}
	}
}
