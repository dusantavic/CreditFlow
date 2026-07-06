using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Mapping;
using CreditFlow.Domain.LoanApplications;
using MediatR;

namespace CreditFlow.Application.LoanApplications.Queries.GetApplicationById
{
	public sealed class GetLoanApplicationByIdQueryHandler
		: IRequestHandler<GetLoanApplicationByIdQuery, LoanApplicationDetailsDto>
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository; 

		public GetLoanApplicationByIdQueryHandler(ILoanApplicationRepository loanApplicationRepository)
		{
			_loanApplicationRepository = loanApplicationRepository; 
		}
		public async Task<LoanApplicationDetailsDto> Handle(
			GetLoanApplicationByIdQuery request, CancellationToken cancellationToken)
		{
			var loanApplication = await _loanApplicationRepository.GetByIdAsync(request.LoanApplicationId, cancellationToken)
				?? throw new NotFoundException(nameof(LoanApplication), request.LoanApplicationId);

			return new LoanApplicationDetailsDto(
				loanApplication.Id,
				loanApplication.ApplicantId,
				loanApplication.RequestedAmount.Amount,
				loanApplication.RequestedAmount.Currency,
				loanApplication.RequestedTermMonths,
				loanApplication.Purpose,
				loanApplication.Status.ToString(),
				loanApplication.SubmittedAtUtc,
				loanApplication.CreditAssessment?.ToDto(),
				loanApplication.ApprovedTerms?.ToDto(),
				loanApplication.RejectionReasons);
		}

	}
}
