
using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.LoanApplications;

namespace CreditFlow.Application.Common.Interfaces
{
	public interface IApplicantRepository
	{
		Task<Applicant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
		Task AddAsync(Applicant applicant, CancellationToken cancellationToken = default); 
	}
}
