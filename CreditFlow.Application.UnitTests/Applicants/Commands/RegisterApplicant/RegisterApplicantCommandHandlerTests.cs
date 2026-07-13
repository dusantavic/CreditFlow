using CreditFlow.Application.Applicants.Commands.RegisterApplicant;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.Applicants;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.Applicants.Commands.RegisterApplicant
{
	public class RegisterApplicantCommandHandlerTests
	{
		private readonly IApplicantRepository _applicantRepository = Substitute.For<IApplicantRepository>();
		private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
		private readonly RegisterApplicantCommandHandler _handler;

		public RegisterApplicantCommandHandlerTests()
		{
			_handler = new RegisterApplicantCommandHandler(_applicantRepository, _unitOfWork);
		}

		private static RegisterApplicantCommand ValidCommand() => new(
			FirstName: "John",
			LastName: "Doe",
			DateOfBirth: new DateOnly(1990, 5, 15),
			NationalId: "1234567890",
			EmployerName: "Acme Corp",
			MonthlyIncome: 4_000m,
			YearsEmployed: 5,
			Currency: "EUR");

		[Fact]
		public async Task Handle_WithValidCommand_AddsApplicantBuiltFromCommandAndSavesOnce()
		{
			Applicant? addedApplicant = null;
			await _applicantRepository.AddAsync(
				Arg.Do<Applicant>(a => addedApplicant = a), Arg.Any<CancellationToken>());

			await _handler.Handle(ValidCommand(), CancellationToken.None);

			addedApplicant.Should().NotBeNull();
			addedApplicant!.PersonalInfo.FullName.Should().Be("John Doe");
			addedApplicant.PersonalInfo.NationalId.Should().Be("1234567890");
			addedApplicant.EmploymentInfo.EmployerName.Should().Be("Acme Corp");
			addedApplicant.EmploymentInfo.MonthlyIncome.Amount.Should().Be(4_000m);
			addedApplicant.EmploymentInfo.MonthlyIncome.Currency.Should().Be("EUR");
			addedApplicant.EmploymentInfo.YearsEmployed.Should().Be(5);
			await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WithValidCommand_ReturnsIdOfAddedApplicant()
		{
			Applicant? addedApplicant = null;
			await _applicantRepository.AddAsync(
				Arg.Do<Applicant>(a => addedApplicant = a), Arg.Any<CancellationToken>());

			var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

			result.Should().NotBeEmpty();
			result.Should().Be(addedApplicant!.Id);
		}

		[Fact]
		public async Task Handle_WithValidCommand_StartsApplicantWithUnverifiedZeroDebt()
		{
			Applicant? addedApplicant = null;
			await _applicantRepository.AddAsync(
				Arg.Do<Applicant>(a => addedApplicant = a), Arg.Any<CancellationToken>());

			await _handler.Handle(ValidCommand(), CancellationToken.None);

			addedApplicant!.FinancialObligations.ExistingMonthlyDebt.Amount.Should().Be(0m);
			addedApplicant.FinancialObligations.ExistingMonthlyDebt.Currency.Should().Be("EUR");
			addedApplicant.FinancialObligations.LastCheckedAtUtc.Should().BeNull();
		}
	}
}
