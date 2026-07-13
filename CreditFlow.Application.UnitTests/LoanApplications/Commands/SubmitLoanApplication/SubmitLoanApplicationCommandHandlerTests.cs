using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.LoanApplications.Commands.SubmitLoanApplication;
using CreditFlow.Domain.LoanApplications;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Commands.SubmitLoanApplication
{
	public class SubmitLoanApplicationCommandHandlerTests
	{
		private readonly IApplicantRepository _applicantRepository = Substitute.For<IApplicantRepository>();
		private readonly ILoanApplicationRepository _loanApplicationRepository = Substitute.For<ILoanApplicationRepository>();
		private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
		private readonly SubmitLoanApplicationCommandHandler _handler;

		public SubmitLoanApplicationCommandHandlerTests()
		{
			_handler = new SubmitLoanApplicationCommandHandler(
				_applicantRepository, _loanApplicationRepository, _unitOfWork);
		}

		private static SubmitLoanApplicationCommand ValidCommand(Guid applicantId) => new(
			ApplicantId: applicantId,
			RequestedAmount: 10_000m,
			Currency: "EUR",
			RequestedTermMonths: 12,
			Purpose: "Car purchase");

		[Fact]
		public async Task Handle_WhenApplicantExists_AddsSubmittedApplicationAndSavesOnce()
		{
			var applicantId = Guid.NewGuid();
			_applicantRepository.ExistsAsync(applicantId, Arg.Any<CancellationToken>()).Returns(true);

			LoanApplication? addedApplication = null;
			await _loanApplicationRepository.AddAsync(
				Arg.Do<LoanApplication>(a => addedApplication = a), Arg.Any<CancellationToken>());

			var result = await _handler.Handle(ValidCommand(applicantId), CancellationToken.None);

			addedApplication.Should().NotBeNull();
			addedApplication!.ApplicantId.Should().Be(applicantId);
			addedApplication.RequestedAmount.Amount.Should().Be(10_000m);
			addedApplication.RequestedAmount.Currency.Should().Be("EUR");
			addedApplication.RequestedTermMonths.Should().Be(12);
			addedApplication.Purpose.Should().Be("Car purchase");
			addedApplication.Status.Should().Be(LoanApplicationStatus.Submitted);
			result.Should().Be(addedApplication.Id);
			await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WhenApplicantDoesNotExist_ThrowsNotFoundException()
		{
			var applicantId = Guid.NewGuid();
			_applicantRepository.ExistsAsync(applicantId, Arg.Any<CancellationToken>()).Returns(false);

			var act = () => _handler.Handle(ValidCommand(applicantId), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"Applicant with key '{applicantId}' was not found.");
		}

		[Fact]
		public async Task Handle_WhenApplicantDoesNotExist_DoesNotAddOrSaveAnything()
		{
			var applicantId = Guid.NewGuid();
			_applicantRepository.ExistsAsync(applicantId, Arg.Any<CancellationToken>()).Returns(false);

			var act = () => _handler.Handle(ValidCommand(applicantId), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>();
			await _loanApplicationRepository.DidNotReceive()
				.AddAsync(Arg.Any<LoanApplication>(), Arg.Any<CancellationToken>());
			await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
		}
	}
}
