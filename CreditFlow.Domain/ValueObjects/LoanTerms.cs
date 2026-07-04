using CreditFlow.Domain.Common;
using CreditFlow.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Represents the concrete terms of an approved loan: principal amount,
	/// interest rate, and repayment period. Calculates the monthly payment
	/// itself using the standard amortization formula, so this logic lives in
	/// one place instead of being duplicated across handlers or UI code.
	/// </summary>
	public sealed class LoanTerms : ValueObject
	{
		public Money PrincipalAmount { get; }
		public InterestRate InterestRate { get; }
		public int TermMonths { get; }
		public Money MonthlyPayment { get; }

		private LoanTerms(Money principalAmount, InterestRate interestRate, int termMonths, Money monthlyPayment)
		{
			PrincipalAmount = principalAmount;
			InterestRate = interestRate;
			TermMonths = termMonths;
			MonthlyPayment = monthlyPayment; 
		}

		public static LoanTerms Of(Money principalAmount, InterestRate interestRate, int termMonths)
		{
			if (termMonhts <= 0) 
				throw new BusinessRuleViolationException("Loan term must be at least 1 month.");

			if (principalAmount.Amount <= 0)
				throw new BusinessRuleViolationException("Loan principal must be greater than zero.");

			var monthlyPayment = CalculateMonthlyPayment(principalAmount, interestRate, termMonths);
			return new LoanTerms(principalAmount, interestRate, termMonths, monthlyPayment);
		}

		// Standard amortization formula: M = P * r(1+r)^n / ((1+r)^n - 1)
		// where r = monthly interest rate (as a fraction) and n = number of months.
		//
		// NOTE: Math.Pow works with double, not decimal (decimal has no built-in
		// exponentiation). We intentionally convert to double only for this one
		// calculation step and immediately round back to 2 decimals via Money.Of.
		// For a real production lending system this precision boundary would need
		// closer scrutiny (or a decimal-safe power implementation); documenting
		// the tradeoff here rather than hiding it
		private static Money CalculateMonthlyPayment(Money principal, InterestRate interestRate, int termMonths)
		{
			var monthlyRate = interestRate.MonthlyRateAsFraction(); //e.g. 0.2

			if (monthlyRate == 0m)
				return principal.Multiply(1m / termMonths);

			var r = (double)monthlyRate;
			var growthFactor = Math.Pow(1 + r, termMonths);
			var paymentAsDouble = (double)principal.Amount * r * growthFactor / (growthFactor - 1);

			return Money.Of((decimal)paymentAsDouble, principal.Currency); 
		}


		// MonthlyPayment is deliberately excluded here: it's fully derived from
		// the three inputs below, so two LoanTerms with the same principal, rate
		// and term will always produce the same payment and therefore already
		// be equal without needing to compare the derived value too.
		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return PrincipalAmount;
			yield return InterestRate;
			yield return TermMonths; 
		}

		public override string ToString()
				=> $"{PrincipalAmount} over {TermMonths} months @ {InterestRate}, {MonthlyPayment}/month";

	}
}
