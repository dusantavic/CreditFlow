
using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.LoanApplications;
using CreditFlow.Domain.ValueObjects;
using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.SubmitLoanApplication
{
	public sealed class SubmitLoanApplicationCommandHandler
		: IRequestHandler<SubmitLoanApplicationCommand, Guid>
	{
		private readonly IApplicantRepository _applicantRepository;
		private readonly ILoanApplicationRepository _loanApplicationRepository;
		private readonly IUnitOfWork _unitOfWork;

		public SubmitLoanApplicationCommandHandler(
		  IApplicantRepository applicantRepository,
		  ILoanApplicationRepository loanApplicationRepository,
		  IUnitOfWork unitOfWork)
		{
			_applicantRepository = applicantRepository;
			_loanApplicationRepository = loanApplicationRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(SubmitLoanApplicationCommand request, CancellationToken cancellationToken)
		{
			// Fail fast with a clear 404 if the applicant doesn't exist, without
			// pulling the full Applicant aggregate into memory just to check.
			var applicantExists = await _applicantRepository.ExistsAsync(request.ApplicantId, cancellationToken); 

			if (!applicantExists)
				throw new NotFoundException(nameof(Applicant), request.ApplicantId);

			var requestedAmount = Money.Of(request.RequestedAmount, request.Currency);

			var loanApplication = LoanApplication.CreateDraft(
				request.ApplicantId,
				requestedAmount,
				request.RequestedTermMonths,
				request.Purpose);

			loanApplication.Submit();

			await _loanApplicationRepository.AddAsync(loanApplication, cancellationToken);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			return loanApplication.Id; 
		}
	}
}
