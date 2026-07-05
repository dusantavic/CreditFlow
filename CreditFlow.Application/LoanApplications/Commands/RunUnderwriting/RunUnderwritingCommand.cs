using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.RunUnderwriting
{
	/// <summary>
	/// Triggers underwriting for a submitted loan application: fetches a
	/// credit bureau report, refreshes the applicant's known financial
	/// obligations with it, evaluates the UnderwritingPolicy, and records the
	/// resulting decision on the application. Returns whether the application
	/// was approved so callers (e.g. a controller) can react without a
	/// separate follow-up query.
	/// </summary>
	public sealed record RunUnderwritingCommand(Guid LoanApplicationId) : IRequest<UnderwritingResultDto>;
}
