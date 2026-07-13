using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class EmploymentInfoTests
	{
		[Fact]
		public void Of_WithValidData_CreatesEmploymentInfo()
		{
			var income = Money.Of(4_000m, "EUR");

			var info = EmploymentInfo.Of("  Acme Corp ", income, 5);

			info.EmployerName.Should().Be("Acme Corp"); // trimmed
			info.MonthlyIncome.Should().Be(income);
			info.YearsEmployed.Should().Be(5);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Of_WithMissingEmployerName_ThrowsBusinessRuleViolationException(string? employerName)
		{
			var act = () => EmploymentInfo.Of(employerName!, Money.Of(4_000m, "EUR"), 5);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*Employer name is required*");
		}

		[Fact]
		public void Of_WithZeroMonthlyIncome_ThrowsBusinessRuleViolationException()
		{
			var act = () => EmploymentInfo.Of("Acme Corp", Money.Zero("EUR"), 5);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*income*greater than zero*");
		}

		[Fact]
		public void Of_WithNegativeYearsEmployed_ThrowsBusinessRuleViolationException()
		{
			var act = () => EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), -1);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*Years employed cannot be negative*");
		}

		[Fact]
		public void Of_WithZeroYearsEmployed_CreatesEmploymentInfo()
		{
			// Zero is valid — someone can be newly employed.
			var info = EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), 0);

			info.YearsEmployed.Should().Be(0);
		}

		[Fact]
		public void Equality_SameComponents_AreEqual()
		{
			var a = EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), 5);
			var b = EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), 5);

			a.Should().Be(b);
		}

		[Fact]
		public void Equality_DifferentIncome_AreNotEqual()
		{
			var a = EmploymentInfo.Of("Acme Corp", Money.Of(4_000m, "EUR"), 5);
			var b = EmploymentInfo.Of("Acme Corp", Money.Of(5_000m, "EUR"), 5);

			a.Should().NotBe(b);
		}
	}
}
