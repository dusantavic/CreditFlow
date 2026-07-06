using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.LoanApplications;
using CreditFlow.Domain.ValueObjects;
using MediatR;


namespace CreditFlow.Application.LoanApplications.Commands.ApproveLoanApplication
{
	public sealed class ApproveLoanApplicationCommandHandler : IRequestHandler<ApproveLoanApplicationCommand>
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository;
		private readonly IUnitOfWork _unitOfWork;

		public ApproveLoanApplicationCommandHandler(
			 ILoanApplicationRepository loanApplicationRepository,
			 IUnitOfWork unitOfWork)
		{
			_loanApplicationRepository = loanApplicationRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task Handle(ApproveLoanApplicationCommand request, CancellationToken cancellationToken)
		{
			var loanApplication = await _loanApplicationRepository.GetByIdAsync(request.LoanApplicationId, cancellationToken)
				?? throw new NotFoundException(nameof(LoanApplication), request.LoanApplicationId);

			var principal = Money.Of(request.ApprovedAmount, request.Currency);
			var interestRate = InterestRate.OfAnnual(request.AnnualInterestRatePercentage);
			var terms = LoanTerms.Of(principal, interestRate, request.TermMonths);

			// The aggregate itself enforces this can only happen from
			// UnderReview status — throws InvalidLoanStateTransitionException
			// otherwise, which the API's middleware maps to 409 Conflict.
			loanApplication.Approve(terms);

			await _unitOfWork.SaveChangesAsync(cancellationToken); 

		}
	}
}
