using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.Common
{
	public interface IDomainEvent : INotification
	{
		DateTime OccurredOnUtc { get; }
	}
}
