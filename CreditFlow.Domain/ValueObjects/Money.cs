using CreditFlow.Domain.Common;
using CreditFlow.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Represents a monetary amount in a specific currency. Prevents the classic
	/// "primitive obsession" bug of adding a decimal amount without checking
	/// that both sides refer to the same currency.
	/// </summary>
	public sealed class Money : ValueObject
	{
		public decimal Amount { get; }
		public string Currency { get; }

		// Private constructor. The only way to create a Money instance is 
		// through the Of(...) factory below
		private Money(decimal amount, string currency)
		{
			Amount = amount;
			Currency = currency;
		}

		// It won't be possible to create Money in invalid state. 
		// The constructor stays private, and the Of(...) method is running through validations. 
		public static Money Of(decimal amount, string currency)
		{
			if (amount < 0)
			{
				throw new BusinessRuleViolationException("A monetary amount cannot be negative."); 
			}

			if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
			{
				throw new BusinessRuleViolationException("Currency must be a 3-letter ISO 4217 code (e.g. USD, EUR).");
			}

			return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), currency.ToUpperInvariant()); 
		}

		public static Money Zero(string currency) => Of(0m, currency);
		
		// Money is immutable: every operation returns a NEW instance instead of
		// mutating the current one. This avoids bugs where two parts of the code
		// hold the same Money reference and one silently changes it under the other.
		public Money Add(Money other)
		{
			EnsureSameCurrencyAs(other);
			return Of(Amount + other.Amount, Currency); 
		}
		public Money Subtract(Money other)
		{
			EnsureSameCurrencyAs(other);
			return Of(Amount - other.Amount, Currency);
		}

		public Money Multiply(decimal factor)
		{
			if (factor < 0)
				throw new BusinessRuleViolationException("Cannot multiply a monetary amount by a negative factor.");

			return Of(Amount * factor, Currency);
		}

		public bool IsGreaterThan(Money other)
		{
			EnsureSameCurrencyAs(other);
			return Amount > other.Amount;
		}

		public bool IsGreaterThanOrEqualTo(Money other)
		{
			EnsureSameCurrencyAs(other);
			return Amount >= other.Amount;
		}

		// Centralizes the currency check so every arithmetic/comparison method
		// above stays short and doesn't repeat the same guard clause.
		public void EnsureSameCurrencyAs(Money other, string? context = null)
		{
			if (Currency != other.Currency)
			{
				var message = context is null
					? $"Cannot operate on amounts in different currencies ({Currency} vs {other.Currency})."
					: $"{context} ({Currency} vs {other.Currency}).";

				throw new BusinessRuleViolationException(message);
			}
		}

		// Required by the ValueObject base class: two Money instances are equal 
		// only if BOTH the amount AND the currency match. 
		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return Amount;
			yield return Currency; 
		}

		public override string ToString() => $"{Amount:N2} {Currency}";
	}
}
