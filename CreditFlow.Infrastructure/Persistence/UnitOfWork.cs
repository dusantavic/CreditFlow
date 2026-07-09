

using CreditFlow.Application.Common.Interfaces;

namespace CreditFlow.Infrastructure.Persistence
{
	/// <summary>
	/// Thin wrapper around DbContext.SaveChangesAsync. Exists so the
	/// Application layer depends only on IUnitOfWork, never on DbContext or
	/// EF Core directly — swapping persistence technology later would mean
	/// changing this implementation, not every handler that calls it.
	/// </summary>
	public sealed class UnitOfWork : IUnitOfWork
	{
		private readonly CreditFlowDbContext _dbContext;

		public UnitOfWork(CreditFlowDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
			=> _dbContext.SaveChangesAsync(cancellationToken); 
	}
}
