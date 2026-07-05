
using CreditFlow.Domain.Common;
using CreditFlow.Domain.Exceptions;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Represents a credit score on the standard 300-850 scale (FICO-style).
	/// Encapsulating the valid range here means no code path can ever construct
	/// or store an out-of-range score.
	/// </summary>
	public sealed class CreditScore : ValueObject
	{
		private const int MinValue = 300;
		private const int MaxValue = 850;

		public int Value { get; }

		private CreditScore(int value)
		{
			Value = value; 
		}

		public static CreditScore Of(int value)
		{
			if (value < MinValue || value > MaxValue)
				throw new BusinessRuleViolationException(
					$"Credit score must be between {MinValue} and {MaxValue}.");

			return new CreditScore(value); 
		}

		public bool IsAtLeast(int threshold) => Value >= threshold;

		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return Value; 
		}

		public override string ToString()
		{
			return Value.ToString(); 
		}
	}
}
