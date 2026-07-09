using CreditFlow.Domain.Common;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// A point-in-time snapshot of an applicant's creditworthiness for a specific
	/// loan application. Immutable by nature — re-running underwriting produces
	/// a NEW assessment rather than mutating this one, which is why this is a
	/// Value Object rather than an Entity with its own tracked identity.
	/// </summary>
	public sealed class CreditAssessment : ValueObject
	{
		public CreditScore CreditScore { get; }
		public RiskTier RiskTier { get; }
		public Percentage DebtToIncomeRatio { get; }
		public IReadOnlyList<string> Reasons { get; }
		public DateTime AssessedAtUtc { get; }

		private CreditAssessment() { } // EF Core

		private CreditAssessment(
			CreditScore creditScore,
			RiskTier riskTier,
			Percentage debtToIncomeRatio,
			IReadOnlyList<string> reasons,
			DateTime assessedAtUtc)
		{
			CreditScore = creditScore;
			RiskTier = riskTier;
			DebtToIncomeRatio = debtToIncomeRatio;
			Reasons = reasons;
			AssessedAtUtc = assessedAtUtc;
		}

		public static CreditAssessment Of(
			CreditScore creditScore,
			RiskTier riskTier,
			Percentage debtToIncomeRatio,
			IReadOnlyList<string> reasons)
		{
			return new CreditAssessment(
				creditScore,
				riskTier,
				debtToIncomeRatio,
				reasons,
				DateTime.UtcNow);
		}

		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return CreditScore;
			yield return RiskTier;
			yield return DebtToIncomeRatio;
			// Reasons and AssessedAtUtc are intentionally excluded from equality:
			// two assessments with the same score/tier/DTI are the "same"
			// business outcome even if computed at slightly different timestamps
			// or with reasons phrased in a different order.
			yield return AssessedAtUtc.Date; 
		}

		public override string ToString()
				=> $"{RiskTier} (score {CreditScore}, DTI {DebtToIncomeRatio})";
	}
}
