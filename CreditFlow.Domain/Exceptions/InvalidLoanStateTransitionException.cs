using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.Exceptions
{
	public sealed class InvalidLoanStateTransitionException : DomainException
	{
		public InvalidLoanStateTransitionException(string currentStatus, string attemptedAction)
			: base ($"Cannot perform '{attemptedAction}' while the loan application is in status '{currentStatus}'.")
		{

		}
	}
}
