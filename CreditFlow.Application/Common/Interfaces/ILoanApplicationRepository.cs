
using CreditFlow.Application.Common.Models;
using CreditFlow.Domain.LoanApplications;

namespace CreditFlow.Application.Common.Interfaces
{
	public interface ILoanApplicationRepository
	{
		Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
		Task AddAsync(LoanApplication loanApplication, CancellationToken cancellationToken = default);
	}
}
