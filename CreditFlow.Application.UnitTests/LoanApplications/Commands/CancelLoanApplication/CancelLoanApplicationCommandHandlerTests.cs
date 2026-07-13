using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.LoanApplications.Commands.CancelLoanApplication;
using CreditFlow.Application.UnitTests.TestHelpers;
using CreditFlow.Domain.LoanApplications;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Commands.CancelLoanApplication
{
	public class CancelLoanApplicationCommandHandlerTests
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository = Substitute.For<ILoanApplicationRepository>();
		private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
		private readonly CancelLoanApplicationCommandHandler _handler;

		public CancelLoanApplicationCommandHandlerTests()
		{
			_handler = new CancelLoanApplicationCommandHandler(_loanApplicationRepository, _unitOfWork);
		}

		[Fact]
		public async Task Handle_WithSubmittedApplication_CancelsAndSavesOnce()
		{
			var application = DomainFixtures.CreateLoanApplication(LoanApplicationStatus.Submitted);
			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);

			await _handler.Handle(new CancelLoanApplicationCommand(application.Id), CancellationToken.None);

			application.Status.Should().Be(LoanApplicationStatus.Cancelled);
			await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
		{
			var missingId = Guid.NewGuid();
			_loanApplicationRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
				.Returns((LoanApplication?)null);

			var act = () => _handler.Handle(new CancelLoanApplicationCommand(missingId), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"LoanApplication with key '{missingId}' was not found.");
			await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
		}
	}
}
