
using FluentValidation;

namespace CreditFlow.Application.Applicants.Commands.RegisterApplicant
{
	// <summary>
	/// Syntactic validation only — required fields, formats, ranges. Business
	/// rules (e.g. "must be at least 18") deliberately live in PersonalInfo.Of()
	/// in the Domain layer, not duplicated here. This validator exists to give
	/// fast, clear feedback on malformed input BEFORE the domain layer is
	/// even touched, and to produce field-level error messages for the API.
	/// </summary>
	public sealed class RegisterApplicantCommandValidator : AbstractValidator<RegisterApplicantCommand>
	{
		public RegisterApplicantCommandValidator()
		{
			RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
			RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
			RuleFor(x => x.NationalId).NotEmpty().MaximumLength(20);

			RuleFor(x => x.DateOfBirth)
				.LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
				.WithMessage("Date of birth must be in the past.");

			RuleFor(x => x.EmployerName).NotEmpty().MaximumLength(200);

			RuleFor(x => x.MonthlyIncome).GreaterThan(0);

			RuleFor(x => x.YearsEmployed).GreaterThanOrEqualTo(0);

			RuleFor(x => x.Currency)
				.NotEmpty()
				.Length(3)
				.WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. USD, EUR).");
		}
	}
}
