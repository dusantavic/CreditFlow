
using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Mapping;
using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.LoanApplications;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Domain.ValueObjects;
using MediatR;

namespace CreditFlow.Application.LoanApplications.Commands.RunUnderwriting
{
	public sealed class RunUnderwritingCommandHandler : IRequestHandler<RunUnderwritingCommand, UnderwritingResultDto>
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository;
		private readonly IApplicantRepository _applicantRepository;
		private readonly ICreditBureauService _creditBureauService;
		private readonly IUnderwritingPolicy _underwritingPolicy;
		private readonly IUnitOfWork _unitOfWork;

		public RunUnderwritingCommandHandler(
			ILoanApplicationRepository loanApplicationRepository,
			IApplicantRepository applicantRepository,
			ICreditBureauService creditBureauService,
			IUnderwritingPolicy underwritingPolicy,
			IUnitOfWork unitOfWork)
		{
			_loanApplicationRepository = loanApplicationRepository;
			_applicantRepository = applicantRepository;
			_creditBureauService = creditBureauService;
			_underwritingPolicy = underwritingPolicy;
			_unitOfWork = unitOfWork;
		}

		public async Task<UnderwritingResultDto> Handle(RunUnderwritingCommand request, CancellationToken cancellationToken)
		{
			var loanApplication = await _loanApplicationRepository.GetByIdAsync(request.LoanApplicationId, cancellationToken)
				?? throw new NotFoundException(nameof(LoanApplication), request.LoanApplicationId);

			var applicant = await _applicantRepository.GetByIdAsync(loanApplication.ApplicantId, cancellationToken)
				?? throw new NotFoundException(nameof(Applicant), loanApplication.ApplicantId);

			loanApplication.StartReview();

			var report = await _creditBureauService.GetReportAsync(applicant.Id, cancellationToken);

			applicant.UpdateFinancialObligations(FinancialObligations.Of(report.ExistingMonthlyDebt));

			var decision = _underwritingPolicy.Evaluate(
				applicant,
				loanApplication.RequestedAmount,
				loanApplication.RequestedTermMonths,
				report.CreditScore);

			loanApplication.ApplyUnderwritingDecision(decision);

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			return new UnderwritingResultDto(
				decision.IsApproved,
				decision.Assessment.RiskTier.ToString(),
				decision.Assessment.CreditScore.Value,
				decision.Assessment.DebtToIncomeRatio.Value,
				decision.Reasons,
				decision.ApprovedTerms?.ToDto());
		}
	}
}
