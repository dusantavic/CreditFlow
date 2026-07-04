using CreditFlow.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

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
		public Money ExistingMonthlyDept { get; }
		private FinancialObligations(Money existingMonthlyDebt)
		{
			ExistingMonthlyDebt = existingMonthlyDebt;
		}

		public static FinancialObligations Of(Money existingMonthlyDebt)
			=> new(existingMonthlyDebt);

		public static FinancialObligations None(string currency)
			=> new(Money.Zero(currency));

		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return ExistingMonthlyDebt;
		}

		public override string ToString() => $"{ExistingMonthlyDebt}/month in existing debt";
	}
}
