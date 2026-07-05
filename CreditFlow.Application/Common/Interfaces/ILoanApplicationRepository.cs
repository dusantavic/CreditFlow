
using CreditFlow.Domain.LoanApplications;

namespace CreditFlow.Application.Common.Interfaces
{
	public interface ILoanApplicationRepository
	{
		Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
		Task AddAsync(LoanApplication loanApplication, CancellationToken cancellationToken = default);
		
		// No explicit Update method: EF Core's change tracker detects
		// modifications to an already-loaded, tracked entity automatically.
		// Update() would be misleading here since it wouldn't actually do
		// anything beyond what tracking already does.
	}
}
