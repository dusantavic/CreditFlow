
using CreditFlow.Application.Common.Models;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Application.Common.Interfaces
{
	/// <summary>
	/// Abstraction over an external credit bureau lookup. The real
	/// implementation would call a paid third-party API; this project ships a
	/// simulated implementation instead.
	/// </summary>
	public interface ICreditBureauService
	{
		Task<CreditBureauReport> GetReportAsync(Guid applicantId, CancellationToken cancellationToken = default); 
	}
}
