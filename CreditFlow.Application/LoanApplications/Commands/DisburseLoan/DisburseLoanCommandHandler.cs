using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.LoanApplications;
using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.DisburseLoan
{
	public sealed class DisburseLoanCommandHandler : IRequestHandler<DisburseLoanCommand>
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository;
		private readonly IUnitOfWork _unitOfWork;

		public DisburseLoanCommandHandler(
			ILoanApplicationRepository loanApplicationRepository,
			IUnitOfWork unitOfWork)
		{
			_loanApplicationRepository = loanApplicationRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task Handle(DisburseLoanCommand request, CancellationToken cancellationToken)
		{
			var loanApplication = await _loanApplicationRepository.GetByIdAsync(request.LoanApplicationId, cancellationToken)
				?? throw new NotFoundException(nameof(LoanApplication), request.LoanApplicationId);

			loanApplication.Disburse();

			await _unitOfWork.SaveChangesAsync(cancellationToken);
		}
	}
}
