using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.LoanApplications;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Commands.RejectLoanApplication
{
	public sealed class RejectLoanApplicationCommandHandler : IRequestHandler<RejectLoanApplicationCommand>
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository;
		private readonly IUnitOfWork _unitOfWork;

		public RejectLoanApplicationCommandHandler(
			ILoanApplicationRepository loanApplicationRepository,
			IUnitOfWork unitOfWork)
		{
			_loanApplicationRepository = loanApplicationRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task Handle(RejectLoanApplicationCommand request, CancellationToken cancellationToken)
		{
			var loanApplication = await _loanApplicationRepository.GetByIdAsync(request.LoanApplicationId, cancellationToken)
				?? throw new NotFoundException(nameof(LoanApplication), request.LoanApplicationId);

			loanApplication.Reject(request.Reasons);

			await _unitOfWork.SaveChangesAsync(cancellationToken);
		}
	}
}
