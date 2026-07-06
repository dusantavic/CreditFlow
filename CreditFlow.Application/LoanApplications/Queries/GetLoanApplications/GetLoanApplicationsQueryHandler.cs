using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Models;
using MediatR;

namespace CreditFlow.Application.LoanApplications.Queries.GetLoanApplications
{
	public sealed class GetLoanApplicationsQueryHandler
		: IRequestHandler<GetLoanApplicationsQuery, PagedResult<LoanApplicationSummaryDto>>
	{
		private readonly ILoanApplicationQueryService _queryService; 
		public GetLoanApplicationsQueryHandler(ILoanApplicationQueryService queryService)
		{
			_queryService = queryService; 
		}

		public Task<PagedResult<LoanApplicationSummaryDto>> Handle(
		 GetLoanApplicationsQuery request, CancellationToken cancellationToken)
		{
			var filter = new LoanApplicationFilter(
				request.Status,
				request.ApplicantId,
				request.SubmittedFromUtc,
				request.SubmittedToUtc);

			// No mapping here at all — the query service already returns
			// exactly the DTO shape the handler needs to hand back.
			return _queryService.GetPagedAsync(filter, request.PageNumber, request.PageSize, cancellationToken);
		}
	}
}
