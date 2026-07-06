using MediatR;


namespace CreditFlow.Application.LoanApplications.Queries.GetApplicationById
{
	public sealed record GetLoanApplicationByIdQuery(Guid LoanApplicationId) : IRequest<LoanApplicationDetailsDto>;
}
