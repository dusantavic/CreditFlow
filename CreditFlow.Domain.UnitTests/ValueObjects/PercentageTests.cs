using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class PercentageTests
	{
		[Theory]
		[InlineData(0)]
		[InlineData(45.5)]
		[InlineData(100)]
		public void Of_WithValueInValidRange_CreatesPercentage(decimal value)
		{
			var percentage = Percentage.Of(value);

			percentage.Value.Should().Be(value);
		}

		[Theory]
		[InlineData(-0.01)]
		[InlineData(-50)]
		[InlineData(100.01)]
		[InlineData(200)]
		public void Of_WithValueOutsideValidRange_ThrowsBusinessRuleViolationException(decimal invalidValue)
		{
			var act = () => Percentage.Of(invalidValue);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*between 0 and 100*");
		}

		[Fact]
		public void Of_RoundsToTwoDecimalPlaces()
		{
			var percentage = Percentage.Of(45.567m);

			percentage.Value.Should().Be(45.57m);
		}

		[Fact]
		public void AsFraction_ReturnsValueDividedByHundred()
		{
			var percentage = Percentage.Of(45.5m);

			percentage.AsFraction().Should().Be(0.455m);
		}

		[Fact]
		public void IsGreaterThan_WithSmallerOther_ReturnsTrue()
		{
			Percentage.Of(50m).IsGreaterThan(Percentage.Of(45m)).Should().BeTrue();
		}

		[Fact]
		public void IsGreaterThan_WithEqualOther_ReturnsFalse()
		{
			Percentage.Of(45m).IsGreaterThan(Percentage.Of(45m)).Should().BeFalse();
		}

		[Fact]
		public void IsLessThanOrEqualTo_WithEqualOther_ReturnsTrue()
		{
			Percentage.Of(45m).IsLessThanOrEqualTo(Percentage.Of(45m)).Should().BeTrue();
		}

		[Fact]
		public void IsLessThanOrEqualTo_WithSmallerOther_ReturnsFalse()
		{
			Percentage.Of(50m).IsLessThanOrEqualTo(Percentage.Of(45m)).Should().BeFalse();
		}

		[Fact]
		public void Equality_SameValue_AreEqual()
		{
			var a = Percentage.Of(33.33m);
			var b = Percentage.Of(33.33m);

			a.Should().Be(b);
		}

		[Fact]
		public void Equality_DifferentValues_AreNotEqual()
		{
			Percentage.Of(33.33m).Should().NotBe(Percentage.Of(33.34m));
		}
	}
}
