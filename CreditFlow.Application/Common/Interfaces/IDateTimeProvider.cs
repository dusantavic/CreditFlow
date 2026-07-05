

namespace CreditFlow.Application.Common.Interfaces
{

	/// <summary>
	/// Abstraction over the current time. Used instead of calling
	/// DateTime.UtcNow directly in handlers, so tests can supply a fixed,
	/// predictable time instead of depending on the real clock.
	/// </summary>
	public interface IDateTimeProvider
	{
		DateTime UtcNow { get; }
	}
}
