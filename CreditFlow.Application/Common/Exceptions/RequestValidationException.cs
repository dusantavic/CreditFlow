

namespace CreditFlow.Application.Common.Exceptions
{
	public sealed class RequestValidationException : Exception
	{
		public IDictionary<string, string[]> Errors { get; }

		public RequestValidationException(IDictionary<string, string[]> errors)
			: base("One or more validation failures occurred.")
		{
			Errors = errors;
		}
	}
}
