
using FluentValidation;

namespace CreditFlow.Application.LoanApplications.Commands.RejectLoanApplication
{
	public sealed class RejectLoanApplicationCommandValidator : AbstractValidator<RejectLoanApplicationCommand>
	{
		public RejectLoanApplicationCommandValidator()
		{
			RuleFor(x => x.LoanApplicationId).NotEmpty();

			RuleFor(x => x.Reasons)
				.NotEmpty()
				.WithMessage("At least one rejection reason is required.");

			RuleForEach(x => x.Reasons)
				.NotEmpty()
				.WithMessage("A rejection reason cannot be blank.");
		}
	}
}
