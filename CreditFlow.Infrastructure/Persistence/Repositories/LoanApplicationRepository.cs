using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.LoanApplications;
using Microsoft.EntityFrameworkCore;


namespace CreditFlow.Infrastructure.Persistence.Repositories
{
	public sealed class LoanApplicationRepository : ILoanApplicationRepository
	{
		private readonly CreditFlowDbContext _dbContext;

		public LoanApplicationRepository(CreditFlowDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
			=> await _dbContext.LoanApplications.FirstOrDefaultAsync(la => la.Id == id, cancellationToken);

		public async Task AddAsync(LoanApplication loanApplication, CancellationToken cancellationToken = default)
			=> await _dbContext.LoanApplications.AddAsync(loanApplication, cancellationToken); 
	}
}
