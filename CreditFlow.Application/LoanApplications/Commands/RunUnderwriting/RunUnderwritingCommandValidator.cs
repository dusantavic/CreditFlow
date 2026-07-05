using FluentValidation;

namespace CreditFlow.Application.LoanApplications.Commands.RunUnderwriting
{
	public sealed class RunUnderwritingCommandValidator : AbstractValidator<RunUnderwritingCommand>
	{
		public RunUnderwritingCommandValidator()
		{
			RuleFor(x => x.LoanApplicationId).NotEmpty(); 
		}
	}
}
