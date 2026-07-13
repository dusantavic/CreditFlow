using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.LoanApplications.Commands.DisburseLoan;
using CreditFlow.Application.UnitTests.TestHelpers;
using CreditFlow.Domain.LoanApplications;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Commands.DisburseLoan
{
	public class DisburseLoanCommandHandlerTests
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository = Substitute.For<ILoanApplicationRepository>();
		private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
		private readonly DisburseLoanCommandHandler _handler;

		public DisburseLoanCommandHandlerTests()
		{
			_handler = new DisburseLoanCommandHandler(_loanApplicationRepository, _unitOfWork);
		}

		[Fact]
		public async Task Handle_WithApprovedApplication_DisbursesAndSavesOnce()
		{
			var application = DomainFixtures.CreateLoanApplication(LoanApplicationStatus.Approved);
			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);

			await _handler.Handle(new DisburseLoanCommand(application.Id), CancellationToken.None);

			application.Status.Should().Be(LoanApplicationStatus.Disbursed);
			await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
		{
			var missingId = Guid.NewGuid();
			_loanApplicationRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
				.Returns((LoanApplication?)null);

			var act = () => _handler.Handle(new DisburseLoanCommand(missingId), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"LoanApplication with key '{missingId}' was not found.");
			await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
		}
	}
}
