
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Application.Common.Models
{
	/// <summary>
	/// The result of a credit bureau lookup: the applicant's current credit
	/// score AND their existing monthly debt obligations as known by the
	/// bureau. Modeled as an Application-layer type (not Domain) because it
	/// represents the shape of an external service response, not a domain
	/// concept in its own right — even though it's composed of Domain value
	/// objects (CreditScore, Money).
	/// </summary>
	public sealed class CreditBureauReport
	{
		public CreditScore CreditScore { get; }
		public Money ExistingMonthlyDebt { get; }

		public CreditBureauReport(CreditScore creditScore, Money existingMonthlyDebt)
		{
			CreditScore = creditScore;
			ExistingMonthlyDebt = existingMonthlyDebt;
		}
	}
}
