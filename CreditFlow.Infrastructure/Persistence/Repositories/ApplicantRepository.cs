using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.Applicants;
using Microsoft.EntityFrameworkCore;

namespace CreditFlow.Infrastructure.Persistence.Repositories
{
	public sealed class ApplicantRepository : IApplicantRepository
	{
		private readonly CreditFlowDbContext _dbContext;

		public ApplicantRepository(CreditFlowDbContext dbContext)
		{
			_dbContext = dbContext; 	
		}

		public async Task<Applicant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
			=> await _dbContext.Applicants.FirstOrDefaultAsync(a => a.Id == id,  cancellationToken);

		public async Task AddAsync(Applicant applicant, CancellationToken cancellationToken = default)
			=> await _dbContext.Applicants.AddAsync(applicant, cancellationToken);

		public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
			=> await _dbContext.Applicants.AnyAsync(a => a.Id == id, cancellationToken);

	}
}
