using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class InterestRateTests
	{
		[Fact]
		public void OfAnnual_WithDecimalValue_CreatesRate()
		{
			var rate = InterestRate.OfAnnual(5.5m);

			rate.AnnualRate.Value.Should().Be(5.5m);
		}

		[Fact]
		public void OfAnnual_WithPercentage_CreatesRate()
		{
			var percentage = Percentage.Of(8.9m);

			var rate = InterestRate.OfAnnual(percentage);

			rate.AnnualRate.Should().Be(percentage);
		}

		[Fact]
		public void OfAnnual_WithValueAbove100_ThrowsBusinessRuleViolationException()
		{
			var act = () => InterestRate.OfAnnual(101m);

			act.Should().Throw<BusinessRuleViolationException>();
		}

		[Fact]
		public void MonthlyRateAsFraction_ReturnsAnnualFractionDividedByTwelve()
		{
			var rate = InterestRate.OfAnnual(12m);

			rate.MonthlyRateAsFraction().Should().Be(0.01m);
		}

		[Fact]
		public void MonthlyRateAsFraction_WithNonRoundRate_ReturnsExactDivision()
		{
			var rate = InterestRate.OfAnnual(5.5m);

			rate.MonthlyRateAsFraction().Should().Be(5.5m / 100m / 12m);
		}

		[Fact]
		public void Equality_SameAnnualRate_AreEqual()
		{
			var a = InterestRate.OfAnnual(5.5m);
			var b = InterestRate.OfAnnual(Percentage.Of(5.5m));

			a.Should().Be(b);
		}

		[Fact]
		public void Equality_DifferentAnnualRates_AreNotEqual()
		{
			InterestRate.OfAnnual(5.5m).Should().NotBe(InterestRate.OfAnnual(8.9m));
		}
	}
}
