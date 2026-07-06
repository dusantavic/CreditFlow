
using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.LoanApplications;
using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.CancelLoanApplication
{
	public sealed class CancelLoanApplicationCommandHandler : IRequestHandler<CancelLoanApplicationCommand>
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository;
		private readonly IUnitOfWork _unitOfWork;

		public CancelLoanApplicationCommandHandler(
			   ILoanApplicationRepository loanApplicationRepository,
			   IUnitOfWork unitOfWork)
		{
			_loanApplicationRepository = loanApplicationRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task Handle(CancelLoanApplicationCommand request, CancellationToken cancellationToken)
		{
			var loanApplication = await _loanApplicationRepository.GetByIdAsync(request.LoanApplicationId, cancellationToken)
				?? throw new NotFoundException(nameof(LoanApplication), request.LoanApplicationId);

			loanApplication.Cancel();

			await _unitOfWork.SaveChangesAsync(cancellationToken);
		}
	}
}
