using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class CreditScoreTests
	{
		[Theory]
		[InlineData(300)]
		[InlineData(650)]
		[InlineData(850)]
		public void Of_WithValueInValidRange_CreatesCreditScore(int value)
		{
			var score = CreditScore.Of(value);

			score.Value.Should().Be(value);
		}

		[Theory]
		[InlineData(299)]
		[InlineData(851)]
		[InlineData(0)]
		[InlineData(-100)]
		public void Of_WithValueOutsideValidRange_ThrowsBusinessRuleViolationException(int invalidValue)
		{
			var act = () => CreditScore.Of(invalidValue);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*between 300 and 850*");
		}

		[Fact]
		public void IsAtLeast_WithValueExactlyAtThreshold_ReturnsTrue()
		{
			CreditScore.Of(650).IsAtLeast(650).Should().BeTrue();
		}

		[Fact]
		public void IsAtLeast_WithValueAboveThreshold_ReturnsTrue()
		{
			CreditScore.Of(651).IsAtLeast(650).Should().BeTrue();
		}

		[Fact]
		public void IsAtLeast_WithValueBelowThreshold_ReturnsFalse()
		{
			CreditScore.Of(649).IsAtLeast(650).Should().BeFalse();
		}

		[Fact]
		public void Equality_SameValue_AreEqual()
		{
			CreditScore.Of(700).Should().Be(CreditScore.Of(700));
		}

		[Fact]
		public void Equality_DifferentValues_AreNotEqual()
		{
			CreditScore.Of(700).Should().NotBe(CreditScore.Of(701));
		}
	}
}
