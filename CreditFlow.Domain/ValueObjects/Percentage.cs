using CreditFlow.Domain.Common;
using CreditFlow.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Represents a percentage value between 0 and 100 (inclusive), stored as
	/// its raw numeric form (e.g. 45.5 means 45.5%). Centralizes range
	/// validation so a percentage can never silently be negative or exceed 100
	/// anywhere it's used (DTI ratio, interest rate components, risk margins).
	/// </summary>
	public sealed class Percentage : ValueObject
	{
		public decimal Value { get; }
		private Percentage(decimal value)
		{
			Value = value; 
		}

		public static Percentage Of(decimal value)
		{
			if (value < 0 || value > 100)
				throw new BusinessRuleViolationException("A percentage must be between 0 and 100.");

			return new Percentage(decimal.Round(value, 2, MidpointRounding.ToEven)); 
		}

		// Converts to a 0-1 fraction, useful when multiplying a Money amount
		// by a percentage (e.g. Money.Multiply(percentage.AsFraction())).
		public decimal AsFraction() => Value / 100m;
		public bool IsGreaterThan(Percentage other) => Value > other.Value;
		public bool IsLessThanOrEqualTo(Percentage other) => Value <= other.Value;
		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return Value;
		}

		public override string ToString() => $"{Value:N2}%";
	}
}
