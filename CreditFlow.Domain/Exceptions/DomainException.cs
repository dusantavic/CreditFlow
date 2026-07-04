using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.Exceptions
{
	public abstract class DomainException : Exception
	{
		protected DomainException(string message) : base(message) { }
	}
}
