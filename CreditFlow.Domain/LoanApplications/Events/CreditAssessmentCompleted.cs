using CreditFlow.Domain.Common;

namespace CreditFlow.Domain.LoanApplications.Events
{
	//Record because immutable
	public sealed record CreditAssessmentCompleted(Guid LoanApplicationId, bool WasApproved) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow; 
	}
}
