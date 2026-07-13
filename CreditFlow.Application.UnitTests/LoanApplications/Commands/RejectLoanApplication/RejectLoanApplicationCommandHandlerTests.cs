using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.LoanApplications.Commands.RejectLoanApplication;
using CreditFlow.Application.UnitTests.TestHelpers;
using CreditFlow.Domain.LoanApplications;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Commands.RejectLoanApplication
{
	public class RejectLoanApplicationCommandHandlerTests
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository = Substitute.For<ILoanApplicationRepository>();
		private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
		private readonly RejectLoanApplicationCommandHandler _handler;

		public RejectLoanApplicationCommandHandlerTests()
		{
			_handler = new RejectLoanApplicationCommandHandler(_loanApplicationRepository, _unitOfWork);
		}

		[Fact]
		public async Task Handle_WithApplicationUnderReview_RejectsWithGivenReasonsAndSavesOnce()
		{
			var application = DomainFixtures.CreateLoanApplication(LoanApplicationStatus.UnderReview);
			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);
			var reasons = new[] { "Suspected fraud." };

			await _handler.Handle(new RejectLoanApplicationCommand(application.Id, reasons), CancellationToken.None);

			application.Status.Should().Be(LoanApplicationStatus.Rejected);
			application.RejectionReasons.Should().BeEquivalentTo(reasons);
			await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
		{
			var missingId = Guid.NewGuid();
			_loanApplicationRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
				.Returns((LoanApplication?)null);

			var act = () => _handler.Handle(
				new RejectLoanApplicationCommand(missingId, new[] { "reason" }), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"LoanApplication with key '{missingId}' was not found.");
			await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
		}
	}
}
