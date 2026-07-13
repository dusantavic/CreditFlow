using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class PersonalInfoTests
	{
		private static readonly DateOnly AdultDateOfBirth = new(1990, 5, 15);

		[Fact]
		public void Of_WithValidData_CreatesPersonalInfo()
		{
			var info = PersonalInfo.Of("  John ", " Doe  ", AdultDateOfBirth, " 1234567890 ");

			info.FirstName.Should().Be("John"); // trimmed
			info.LastName.Should().Be("Doe");
			info.DateOfBirth.Should().Be(AdultDateOfBirth);
			info.NationalId.Should().Be("1234567890");
			info.FullName.Should().Be("John Doe");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Of_WithMissingFirstName_ThrowsBusinessRuleViolationException(string? firstName)
		{
			var act = () => PersonalInfo.Of(firstName!, "Doe", AdultDateOfBirth, "1234567890");

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*First name is required*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Of_WithMissingLastName_ThrowsBusinessRuleViolationException(string? lastName)
		{
			var act = () => PersonalInfo.Of("John", lastName!, AdultDateOfBirth, "1234567890");

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*Last name is required*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Of_WithMissingNationalId_ThrowsBusinessRuleViolationException(string? nationalId)
		{
			var act = () => PersonalInfo.Of("John", "Doe", AdultDateOfBirth, nationalId!);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*National ID is required*");
		}

		[Fact]
		public void Of_WithApplicantOneDayUnderEighteen_ThrowsBusinessRuleViolationException()
		{
			// Born 18 years ago tomorrow: today they are exactly 17 years and 364 days old.
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			var dateOfBirth = today.AddYears(-18).AddDays(1);

			var act = () => PersonalInfo.Of("John", "Doe", dateOfBirth, "1234567890");

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*at least 18 years old*");
		}

		[Fact]
		public void Of_WithApplicantExactlyEighteenToday_CreatesPersonalInfo()
		{
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			var dateOfBirth = today.AddYears(-18);

			var info = PersonalInfo.Of("John", "Doe", dateOfBirth, "1234567890");

			info.DateOfBirth.Should().Be(dateOfBirth);
		}

		[Fact]
		public void MaskedNationalId_WithTypicalLengthId_MasksAllButLastFourCharacters()
		{
			var info = PersonalInfo.Of("John", "Doe", AdultDateOfBirth, "1234567890");

			info.MaskedNationalId().Should().Be("******7890");
		}

		[Fact]
		public void MaskedNationalId_WithFourCharacterId_MasksEverything()
		{
			var info = PersonalInfo.Of("John", "Doe", AdultDateOfBirth, "1234");

			info.MaskedNationalId().Should().Be("****");
		}

		[Fact]
		public void MaskedNationalId_WithIdShorterThanFourCharacters_MasksEverything()
		{
			var info = PersonalInfo.Of("John", "Doe", AdultDateOfBirth, "12");

			info.MaskedNationalId().Should().Be("**");
		}

		[Fact]
		public void Equality_SameComponents_AreEqual()
		{
			var a = PersonalInfo.Of("John", "Doe", AdultDateOfBirth, "1234567890");
			var b = PersonalInfo.Of("John", "Doe", AdultDateOfBirth, "1234567890");

			a.Should().Be(b);
		}

		[Fact]
		public void Equality_DifferentNationalId_AreNotEqual()
		{
			var a = PersonalInfo.Of("John", "Doe", AdultDateOfBirth, "1234567890");
			var b = PersonalInfo.Of("John", "Doe", AdultDateOfBirth, "0987654321");

			a.Should().NotBe(b);
		}
	}
}
