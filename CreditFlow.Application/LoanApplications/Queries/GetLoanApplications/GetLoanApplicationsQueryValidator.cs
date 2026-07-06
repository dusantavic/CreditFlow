using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.LoanApplications.Queries.GetLoanApplications
{
	public sealed class GetLoanApplicationsQueryValidator : AbstractValidator<GetLoanApplicationsQuery>
	{
		public GetLoanApplicationsQueryValidator()
		{
			RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
			RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

			RuleFor(x => x)
				.Must(x => x.SubmittedFromUtc is null || x.SubmittedToUtc is null || x.SubmittedFromUtc <= x.SubmittedToUtc)
				.WithMessage("SubmittedFromUtc must be earlier than or equal to SubmittedToUtc.");
		}
	}
}
