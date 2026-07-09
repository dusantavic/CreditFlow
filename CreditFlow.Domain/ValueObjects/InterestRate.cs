using CreditFlow.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Represents an annual interest rate. Wraps Percentage rather than a raw
	/// decimal so it carries its own semantic meaning (this is specifically a
	/// yearly rate, not a generic ratio) and can grow its own behavior later
	/// (e.g. converting to a monthly rate) without affecting other Percentage
	/// usages like DTI ratio.
	/// </summary>
	public sealed class InterestRate : ValueObject
	{
		public Percentage AnnualRate { get; }
		private InterestRate() { } // EF Core

		private InterestRate(Percentage annualRate)
		{
			AnnualRate = annualRate;
		}
		public static InterestRate OfAnnual(decimal annualPercentageValue)
			=> new(Percentage.Of(annualPercentageValue));

		public static InterestRate OfAnnual(Percentage annualRate)
			=> new(annualRate);

		// Needed for monthly installment calculations (e.g. amortization formula),
		// where the rate must be expressed per period, not per year.
		public decimal MonthlyRateAsFraction() => AnnualRate.AsFraction() / 12m;
		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return AnnualRate;
		}

		public override string ToString() => $"{AnnualRate} / year";
	}
}
