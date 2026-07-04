using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.Exceptions
{
	internal class BusinessRuleViolationException : DomainException
	{
		public BusinessRuleViolationException(string message) : base(message) { }
	}
}
