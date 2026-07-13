
using CreditFlow.Domain.Common;

namespace CreditFlow.Domain.LoanApplications.Events
{
	public sealed record LoanApplicationApproved(Guid LoanApplicationId, Guid ApplicantId) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;

	}
}
