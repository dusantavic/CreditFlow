namespace CreditFlow.Domain.Underwriting
{
	/// <summary>
	/// Tunable underwriting thresholds and per-tier settings. Deliberately a
	/// plain POCO with no dependency on Microsoft.Extensions.Options or any
	/// configuration framework — the Domain project stays framework-free.
	/// Binding this from appsettings.json happens in the Infrastructure/Api
	/// layer, which then passes a populated instance into UnderwritingPolicy's
	/// constructor. All numeric business thresholds live here, not as
	/// hardcoded constants in UnderwritingPolicy itself.
	/// </summary>
	public sealed class UnderwritingPolicyOptions
	{
		public int MinAcceptableCreditScore { get; init; } = 580;
		public int NearPrimeMinScore { get; init; } = 650;
		public int PrimeMinScore { get; init; } = 750;
		public decimal MaxAllowedDtiPercentage { get; init; } = 45m;

		public RiskTierSettings Prime { get; init; } = new() { AnnualInterestRate = 5.5m, IncomeMultiplier = 10m };
		public RiskTierSettings NearPrime { get; init; } = new() { AnnualInterestRate = 8.9m, IncomeMultiplier = 6m };
		public RiskTierSettings SubPrime { get; init; } = new() { AnnualInterestRate = 14.9m, IncomeMultiplier = 3m };
	}

	/// <summary>
	/// Interest rate and lending-limit settings for a single risk tier.
	/// </summary>
	public sealed class RiskTierSettings
	{
		public decimal AnnualInterestRate { get; init; }
		public decimal IncomeMultiplier { get; init; }
	}
}
