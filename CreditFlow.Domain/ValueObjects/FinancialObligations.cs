using CreditFlow.Domain.Common;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Represents an applicant's existing financial obligations, independent of
	/// their current employment — debt doesn't disappear when someone changes
	/// jobs, so it doesn't belong grouped with EmploymentInfo. Kept as its own
	/// value object so it can grow (e.g. multiple existing loans, alimony)
	/// without forcing changes to Applicant or EmploymentInfo.
	/// </summary>
	public sealed class FinancialObligations : ValueObject
	{
		public Money ExistingMonthlyDebt { get; }

		/// <summary>
		/// When this debt figure was last verified against an authoritative
		/// source (the credit bureau). Null means it has never been verified —
		/// e.g. the zero-debt default assumed at applicant registration, before
		/// any underwriting has run. A non-null value tells callers exactly how
		/// stale this data is, which matters for decisions that shouldn't rely
		/// on months-old information.
		/// </summary>
		public DateTime? LastCheckedAtUtc { get; }
		private FinancialObligations() { } // EF Core

		private FinancialObligations(Money existingMonthlyDebt, DateTime? lastCheckedAtUtc)
		{
			ExistingMonthlyDebt = existingMonthlyDebt;
			LastCheckedAtUtc = lastCheckedAtUtc;
		}

		/// <summary>
		/// Creates obligations backed by a real check (e.g. a credit bureau
		/// report). Automatically stamps the current time as the verification
		/// moment — callers never pass a timestamp themselves, keeping this
		/// consistent with how CreditAssessment.Of self-stamps AssessedAtUtc.
		/// </summary>
		public static FinancialObligations Of(Money existingMonthlyDebt)
			=> new(existingMonthlyDebt, DateTime.UtcNow);

		/// <summary>
		/// Default state for a newly registered applicant: assumed zero debt,
		/// not yet verified against any authoritative source.
		/// </summary>
		public static FinancialObligations None(string currency)
			=> new(Money.Zero(currency), lastCheckedAtUtc: null);

		public bool IsStale(TimeSpan maxAge)
			=> LastCheckedAtUtc is null || DateTime.UtcNow - LastCheckedAtUtc.Value > maxAge;

		// LastCheckedAtUtc is intentionally excluded from equality: two
		// FinancialObligations with the same debt amount represent the same
		// business fact even if verified at slightly different times. Equality
		// here is about "same debt", not "checked at the same instant".
		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return ExistingMonthlyDebt;
		}

		public override string ToString()
			=> LastCheckedAtUtc is null
				? $"{ExistingMonthlyDebt}/month in existing debt (unverified)"
				: $"{ExistingMonthlyDebt}/month in existing debt (verified {LastCheckedAtUtc:u})";
	}
}
