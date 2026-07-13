using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class LoanTermsTests
	{
		[Fact]
		public void Of_WithStandardTermAndNonZeroRate_CalculatesAmortizedMonthlyPayment()
		{
			// 10,000 at 12% annual over 12 months: monthly rate r = 0.01, n = 12.
			// M = P * r(1+r)^n / ((1+r)^n - 1) = 10000 * 0.01 * 1.01^12 / (1.01^12 - 1) = 888.4879...
			var terms = LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(12m), 12);

			terms.MonthlyPayment.Should().Be(Money.Of(888.49m, "EUR"));
		}

		[Fact]
		public void Of_WithZeroInterestRate_DividesPrincipalByTermMonths()
		{
			var terms = LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(0m), 12);

			terms.MonthlyPayment.Should().Be(Money.Of(833.33m, "EUR"));
		}

		[Fact]
		public void Of_WithZeroPrincipal_ThrowsBusinessRuleViolationException()
		{
			var act = () => LoanTerms.Of(Money.Of(0m, "EUR"), InterestRate.OfAnnual(5.5m), 12);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*principal*greater than zero*");
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-12)]
		public void Of_WithZeroOrNegativeTermMonths_ThrowsBusinessRuleViolationException(int invalidTermMonths)
		{
			var act = () => LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(5.5m), invalidTermMonths);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*term*at least 1 month*");
		}

		[Fact]
		public void Of_CalledTwiceWithIdenticalInputs_ProducesIdenticalMonthlyPayment()
		{
			var first = LoanTerms.Of(Money.Of(25_000m, "EUR"), InterestRate.OfAnnual(8.9m), 60);
			var second = LoanTerms.Of(Money.Of(25_000m, "EUR"), InterestRate.OfAnnual(8.9m), 60);

			first.MonthlyPayment.Should().Be(second.MonthlyPayment);
			first.Should().Be(second);
		}

		[Fact]
		public void Of_SetsAllComponentsAsProvided()
		{
			var principal = Money.Of(15_000m, "USD");
			var rate = InterestRate.OfAnnual(5.5m);

			var terms = LoanTerms.Of(principal, rate, 24);

			terms.PrincipalAmount.Should().Be(principal);
			terms.InterestRate.Should().Be(rate);
			terms.TermMonths.Should().Be(24);
			terms.MonthlyPayment.Currency.Should().Be("USD");
		}

		[Fact]
		public void Equality_SamePrincipalRateAndTerm_AreEqual()
		{
			var a = LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(5.5m), 36);
			var b = LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(5.5m), 36);

			a.Should().Be(b);
		}

		[Fact]
		public void Equality_DifferentTermMonths_AreNotEqual()
		{
			var a = LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(5.5m), 36);
			var b = LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(5.5m), 24);

			a.Should().NotBe(b);
		}
	}
}
