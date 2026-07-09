using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Models;
using CreditFlow.Application.LoanApplications.Queries.GetLoanApplications;
using Microsoft.EntityFrameworkCore;


namespace CreditFlow.Infrastructure.Persistence.Queries
{
	public sealed class LoanApplicationQueryService : ILoanApplicationQueryService
	{
		private readonly CreditFlowDbContext _dbContext;

		public LoanApplicationQueryService(CreditFlowDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<PagedResult<LoanApplicationSummaryDto>> GetPagedAsync(
			LoanApplicationFilter filter, 
			int pageNumber, 
			int pageSize, 
			CancellationToken cancellationToken = default)
		{
			// AsNoTracking: this is a pure read, nothing here is ever modified
			// and saved back, so there's no reason to pay for change tracking.
			var query = _dbContext.LoanApplications.AsNoTracking();

			if (filter.Status is not null)
				query = query.Where(x => x.Status == filter.Status);

			if (filter.ApplicantId is not null)
				query = query.Where(x => x.ApplicantId == filter.ApplicantId);

			if (filter.SubmittedFromUtc is not null)
				query = query.Where(x => x.SubmittedAtUtc >= filter.SubmittedFromUtc);

			if (filter.SubmittedToUtc is not null)
				query = query.Where(x => x.SubmittedAtUtc <= filter.SubmittedToUtc);

			var totalCount = await query.CountAsync(cancellationToken);

			var items = await query
				.OrderByDescending(x => x.SubmittedAtUtc)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(x => new LoanApplicationSummaryDto(
					x.Id,
					x.ApplicantId,
					x.RequestedAmount.Amount,
					x.RequestedAmount.Currency,
					x.Status.ToString(),
					x.CreditAssessment != null ? x.CreditAssessment.RiskTier.ToString() : null,
					x.SubmittedAtUtc
					))
				.ToListAsync(cancellationToken);

			return new PagedResult<LoanApplicationSummaryDto>(items, totalCount, pageNumber, pageSize); 
		}
	}
}
