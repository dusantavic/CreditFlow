
using CreditFlow.Domain.Common;

namespace CreditFlow.Domain.LoanApplications.Events
{
	public sealed record LoanApplicationSubmitted(Guid LoanApplicationId, Guid AppliantId) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
	}
}
