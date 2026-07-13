using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Models;
using CreditFlow.Application.LoanApplications.Queries.GetLoanApplications;
using CreditFlow.Domain.LoanApplications;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Queries.GetLoanApplications
{
	public class GetLoanApplicationsQueryHandlerTests
	{
		private readonly ILoanApplicationQueryService _queryService = Substitute.For<ILoanApplicationQueryService>();
		private readonly GetLoanApplicationsQueryHandler _handler;

		public GetLoanApplicationsQueryHandlerTests()
		{
			_handler = new GetLoanApplicationsQueryHandler(_queryService);
		}

		[Fact]
		public async Task Handle_BuildsFilterFromQueryAndDelegatesToQueryService()
		{
			var applicantId = Guid.NewGuid();
			var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var to = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
			var query = new GetLoanApplicationsQuery(
				LoanApplicationStatus.Submitted, applicantId, from, to, PageNumber: 2, PageSize: 10);

			// LoanApplicationFilter is a record, so Received matches by value equality.
			var expectedFilter = new LoanApplicationFilter(LoanApplicationStatus.Submitted, applicantId, from, to);
			_queryService.GetPagedAsync(expectedFilter, 2, 10, Arg.Any<CancellationToken>())
				.Returns(new PagedResult<LoanApplicationSummaryDto>(Array.Empty<LoanApplicationSummaryDto>(), 0, 2, 10));

			await _handler.Handle(query, CancellationToken.None);

			await _queryService.Received(1).GetPagedAsync(expectedFilter, 2, 10, Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_ReturnsPagedResultFromQueryServiceUnchanged()
		{
			var items = new[]
			{
				new LoanApplicationSummaryDto(
					Guid.NewGuid(), Guid.NewGuid(), 10_000m, "EUR", "Submitted", null, DateTime.UtcNow)
			};
			var pagedResult = new PagedResult<LoanApplicationSummaryDto>(items, totalCount: 1, pageNumber: 1, pageSize: 20);
			_queryService.GetPagedAsync(
					Arg.Any<LoanApplicationFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
				.Returns(pagedResult);

			var result = await _handler.Handle(new GetLoanApplicationsQuery(), CancellationToken.None);

			result.Should().BeSameAs(pagedResult);
		}

		[Fact]
		public async Task Handle_WithNoFilterParameters_PassesAllNullFilter()
		{
			_queryService.GetPagedAsync(
					Arg.Any<LoanApplicationFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
				.Returns(new PagedResult<LoanApplicationSummaryDto>(Array.Empty<LoanApplicationSummaryDto>(), 0, 1, 20));

			await _handler.Handle(new GetLoanApplicationsQuery(), CancellationToken.None);

			await _queryService.Received(1).GetPagedAsync(
				new LoanApplicationFilter(null, null, null, null), 1, 20, Arg.Any<CancellationToken>());
		}
	}
}
