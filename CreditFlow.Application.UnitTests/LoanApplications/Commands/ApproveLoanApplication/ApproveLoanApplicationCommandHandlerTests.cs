using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.LoanApplications.Commands.ApproveLoanApplication;
using CreditFlow.Application.UnitTests.TestHelpers;
using CreditFlow.Domain.LoanApplications;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Commands.ApproveLoanApplication
{
	public class ApproveLoanApplicationCommandHandlerTests
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository = Substitute.For<ILoanApplicationRepository>();
		private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
		private readonly ApproveLoanApplicationCommandHandler _handler;

		public ApproveLoanApplicationCommandHandlerTests()
		{
			_handler = new ApproveLoanApplicationCommandHandler(_loanApplicationRepository, _unitOfWork);
		}

		[Fact]
		public async Task Handle_WithApplicationUnderReview_ApprovesWithTermsBuiltFromCommandAndSavesOnce()
		{
			var application = DomainFixtures.CreateLoanApplication(LoanApplicationStatus.UnderReview);
			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);
			var command = new ApproveLoanApplicationCommand(application.Id, 8_000m, "EUR", 5.5m, 24);

			await _handler.Handle(command, CancellationToken.None);

			application.Status.Should().Be(LoanApplicationStatus.Approved);
			application.ApprovedTerms.Should().Be(
				LoanTerms.Of(Money.Of(8_000m, "EUR"), InterestRate.OfAnnual(5.5m), 24));
			await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
		{
			var missingId = Guid.NewGuid();
			_loanApplicationRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
				.Returns((LoanApplication?)null);
			var command = new ApproveLoanApplicationCommand(missingId, 8_000m, "EUR", 5.5m, 24);

			var act = () => _handler.Handle(command, CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"LoanApplication with key '{missingId}' was not found.");
			await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
		}
	}
}
