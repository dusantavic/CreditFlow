
using CreditFlow.Domain.Applicants;

namespace CreditFlow.Application.Common.Interfaces
{
	public interface IApplicantRepository
	{
		Task<Applicant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
		Task AddAsync(Applicant applicant, CancellationToken cancellationToken = default);
		Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default); 
	}
}
