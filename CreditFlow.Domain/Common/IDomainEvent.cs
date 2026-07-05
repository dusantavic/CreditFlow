using MediatR;

namespace CreditFlow.Domain.Common
{
	public interface IDomainEvent : INotification
	{
		DateTime OccurredOnUtc { get; }
	}
}
