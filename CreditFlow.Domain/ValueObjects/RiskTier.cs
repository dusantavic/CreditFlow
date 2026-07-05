namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Risk classification derived from credit score, driving both the maximum
	/// loan amount and the interest rate premium applied during underwriting.
	/// </summary>
	public enum RiskTier
	{
		Declined = 0, 
		SubPrime = 1, 
		NearPrime = 2, 
		Prime = 3
	}
}
