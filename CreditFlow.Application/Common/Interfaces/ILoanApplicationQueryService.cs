using CreditFlow.Application.Common.Models;
using CreditFlow.Application.LoanApplications.Queries.GetLoanApplications;
using CreditFlow.Domain.LoanApplications;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.Common.Interfaces
{
	/// <summary>
	/// Read-side data access for loan applications. Deliberately separate from
	/// ILoanApplicationRepository: this returns DTOs directly, projected at the
	/// database level (via EF Core's LINQ Select translated to SQL), so a list
	/// query never pays the cost of materializing full LoanApplication
	/// aggregates just to discard most of their data.
	/// </summary>
	public interface ILoanApplicationQueryService
	{
		Task<PagedResult<LoanApplicationSummaryDto>> GetPagedAsync(
			LoanApplicationFilter filter,
			int pageNumber,
			int pageSize,
			CancellationToken cancellationToken = default);

	}
}
