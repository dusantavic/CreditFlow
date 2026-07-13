
using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class MoneyTests
	{
		[Fact]
		public void Of_WithValidAmountAndCurrency_CreatesMoney()
		{
			var money = Money.Of(100.50m, "eur");

			money.Amount.Should().Be(100.50m);
			money.Currency.Should().Be("EUR"); //uppercased
		}

		[Fact]
		public void Of_WithNegativeAmount_ThrowsBusinessRuleViolationException()
		{
			var act = () => Money.Of(-1m, "EUR");

			act.Should().Throw<BusinessRuleViolationException>();
		}

		[Theory]
		[InlineData("")]
		[InlineData("EU")]
		[InlineData("EURO")]
		public void Of_WithInvalidCurrencyCode_ThrowsBusinessRuleViolationException(string invalidCurrency)
		{
			var act = () => Money.Of(100m, invalidCurrency);

			act.Should().Throw<BusinessRuleViolationException>();
		}

		[Fact]
		public void Add_WithSameCurrency_ReturnsSum()
		{
			var a = Money.Of(100m, "EUR");
			var b = Money.Of(50m, "EUR");

			var result = a.Add(b);

			result.Amount.Should().Be(150m);
			result.Currency.Should().Be("EUR");
		}

		[Fact]
		public void Add_WithDifferentCurrencies_ThrowsBusinessRuleViolationException()
		{
			var eur = Money.Of(100m, "EUR");
			var usd = Money.Of(50m, "USD");

			var act = () => eur.Add(usd);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*currencies*");
		}

		[Fact]
		public void Multiply_WithNegativeFactor_ThrowsBusinessRuleViolationException()
		{
			var money = Money.Of(100m, "EUR");

			var act = () => money.Multiply(-2m);

			act.Should().Throw<BusinessRuleViolationException>();
		}

		[Fact]
		public void Equality_SameAmountAndCurrency_AreEqual()
		{
			var a = Money.Of(100m, "EUR");
			var b = Money.Of(100m, "EUR");

			a.Should().Be(b);
			(a == b).Should().BeTrue();
		}

		[Fact]
		public void Equality_SameAmountDifferentCurrency_AreNotEqual()
		{
			var eur = Money.Of(100m, "EUR");
			var usd = Money.Of(100m, "USD");

			eur.Should().NotBe(usd);
		}

		[Fact]
		public void Of_RoundsToTwoDecimalPlaces()
		{
			var money = Money.Of(100.567m, "EUR");

			money.Amount.Should().Be(100.57m);
		}
	}
}
