using CreditFlow.Application.Common.Models;
using CreditFlow.Domain.LoanApplications;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Queries.GetLoanApplications
{
	/// <summary>
	/// Lists loan applications with optional filtering and pagination. All
	/// filter parameters are optional; pagination defaults to a reasonable
	/// first page if not specified.
	/// </summary>
	public sealed record GetLoanApplicationsQuery(
		LoanApplicationStatus? Status = null,
		Guid? ApplicantId = null,
		DateTime? SubmittedFromUtc = null,
		DateTime? SubmittedToUtc = null,
		int PageNumber = 1,
		int PageSize = 20) : IRequest<PagedResult<LoanApplicationSummaryDto>>; 
}
