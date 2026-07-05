
using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Domain.Underwriting
{
	/// <summary>
	/// Domain service abstraction for underwriting decisions. Defined as an
	/// interface (even though there's currently one implementation) so
	/// UnderwritingPolicy can be swapped or extended — e.g. a
	/// RegionSpecificUnderwritingPolicy — without touching any code that
	/// depends on this contract.
	/// </summary>
	public interface IUnderwritingPolicy
	{
		UnderwritingDecision Evaluate(Applicant applicant, Money requestedAmount, int requestedTermMonths, CreditScore creditScore); 
	}
}
