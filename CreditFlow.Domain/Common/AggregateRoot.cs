using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.Common
{
	public abstract class AggregateRoot<TId> : Entity<TId> where TId: notnull
	{
		// Base class for aggregate roots: the only entities a repository loads or
		// persists directly, and the only place domain events are raised from.

		private readonly List<IDomainEvent> _domainEvents = new();

		public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

		protected AggregateRoot() { }
		protected AggregateRoot(TId id) : base(id) { }

		//protected - only aggregate can decide if domain event should be raised
		protected void RaiseDomainEvent(IDomainEvent domainEvent)
		{
			_domainEvents.Add(domainEvent); 
		}

		public void ClearDomainEvents()
		{
			_domainEvents.Clear(); 
		}

	}
}
