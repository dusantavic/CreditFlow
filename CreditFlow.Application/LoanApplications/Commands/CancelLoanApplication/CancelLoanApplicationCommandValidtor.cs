using FluentValidation;


namespace CreditFlow.Application.LoanApplications.Commands.CancelLoanApplication
{
	public sealed class CancelLoanApplicationCommandValidator : AbstractValidator<CancelLoanApplicationCommand>
	{
		public CancelLoanApplicationCommandValidator()
		{
			RuleFor(x => x.LoanApplicationId).NotEmpty();
		}
	}
}
