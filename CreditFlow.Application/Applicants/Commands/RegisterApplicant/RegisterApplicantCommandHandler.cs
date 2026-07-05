
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.ValueObjects;
using MediatR;

namespace CreditFlow.Application.Applicants.Commands.RegisterApplicant
{
	public sealed class RegisterApplicantCommandHandler : IRequestHandler<RegisterApplicantCommand, Guid>
	{
		private readonly IApplicantRepository _applicantRepository;
		private readonly IUnitOfWork _unitOfWork;

		public RegisterApplicantCommandHandler(
			IApplicantRepository applicantRepository,
			IUnitOfWork unitOfWork)
		{
			_applicantRepository = applicantRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(RegisterApplicantCommand request, CancellationToken cancellationToken)
		{
			var personalInfo = PersonalInfo.Of(
				request.FirstName,
				request.LastName,
				request.DateOfBirth,
				request.NationalId
				);

			var employmentInfo = EmploymentInfo.Of(
				request.EmployerName,
				Money.Of(request.MonthlyIncome, request.Currency),
				request.YearsEmployed);

			// No existing debt captured at registration time — Applicant starts with
			// zero obligations and gets refreshed with authoritative data from the
			// credit bureau at underwriting time (see RunUnderwritingCommandHandler),
			// which is when this information is actually current and needed.
			var financialObligations = FinancialObligations.None(request.Currency);

			var applicant = Applicant.Register(personalInfo, employmentInfo, financialObligations);

			await _applicantRepository.AddAsync(applicant, cancellationToken);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			return applicant.Id; 
		}
	}
}
