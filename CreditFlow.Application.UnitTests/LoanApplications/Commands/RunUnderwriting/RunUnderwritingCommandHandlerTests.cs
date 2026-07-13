using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Models;
using CreditFlow.Application.LoanApplications.Commands.RunUnderwriting;
using CreditFlow.Application.UnitTests.TestHelpers;
using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.LoanApplications;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Commands.RunUnderwriting
{
	public class RunUnderwritingCommandHandlerTests
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository = Substitute.For<ILoanApplicationRepository>();
		private readonly IApplicantRepository _applicantRepository = Substitute.For<IApplicantRepository>();
		private readonly ICreditBureauService _creditBureauService = Substitute.For<ICreditBureauService>();
		private readonly IUnderwritingPolicy _underwritingPolicy = Substitute.For<IUnderwritingPolicy>();
		private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
		private readonly RunUnderwritingCommandHandler _handler;

		public RunUnderwritingCommandHandlerTests()
		{
			_handler = new RunUnderwritingCommandHandler(
				_loanApplicationRepository,
				_applicantRepository,
				_creditBureauService,
				_underwritingPolicy,
				_unitOfWork);
		}

		private (LoanApplication Application, Applicant Applicant, CreditBureauReport Report) SetUpHappyPath(
			UnderwritingDecision decision)
		{
			var applicant = DomainFixtures.CreateApplicant();
			var application = DomainFixtures.CreateLoanApplication(
				LoanApplicationStatus.Submitted, applicantId: applicant.Id);
			var report = new CreditBureauReport(CreditScore.Of(780), Money.Of(300m, "EUR"));

			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);
			_applicantRepository.GetByIdAsync(applicant.Id, Arg.Any<CancellationToken>())
				.Returns(applicant);
			_creditBureauService.GetReportAsync(applicant.Id, Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(report));
			_underwritingPolicy.Evaluate(
					Arg.Any<Applicant>(), Arg.Any<Money>(), Arg.Any<int>(), Arg.Any<CreditScore>())
				.Returns(decision);

			return (application, applicant, report);
		}

		[Fact]
		public async Task Handle_WithApprovedDecision_MovesApplicationToApprovedAndReturnsApprovedDto()
		{
			var terms = DomainFixtures.SampleTerms();
			var reasons = new[] { "Approved at risk tier Prime with DTI 20.00%." };
			var (application, _, _) = SetUpHappyPath(DomainFixtures.ApprovedDecision(terms, reasons));

			var result = await _handler.Handle(new RunUnderwritingCommand(application.Id), CancellationToken.None);

			application.Status.Should().Be(LoanApplicationStatus.Approved);
			application.ApprovedTerms.Should().Be(terms);
			application.CreditAssessment.Should().NotBeNull();

			result.IsApproved.Should().BeTrue();
			result.RiskTier.Should().Be("Prime");
			result.CreditScore.Should().Be(780);
			result.DebtToIncomeRatioPercentage.Should().Be(20m);
			result.Reasons.Should().BeEquivalentTo(reasons);
			result.ApprovedTerms.Should().NotBeNull();
			result.ApprovedTerms!.PrincipalAmount.Should().Be(terms.PrincipalAmount.Amount);
			result.ApprovedTerms.Currency.Should().Be(terms.PrincipalAmount.Currency);
			result.ApprovedTerms.AnnualInterestRatePercentage.Should().Be(terms.InterestRate.AnnualRate.Value);
			result.ApprovedTerms.TermMonths.Should().Be(terms.TermMonths);
			result.ApprovedTerms.MonthlyPayment.Should().Be(terms.MonthlyPayment.Amount);
		}

		[Fact]
		public async Task Handle_WithDeclinedDecision_MovesApplicationToRejectedAndReturnsDeclinedDto()
		{
			var reasons = new[] { "Debt-to-income ratio of 86.00% exceeds the maximum allowed 45%." };
			var (application, _, _) = SetUpHappyPath(DomainFixtures.DeclinedDecision(reasons));

			var result = await _handler.Handle(new RunUnderwritingCommand(application.Id), CancellationToken.None);

			application.Status.Should().Be(LoanApplicationStatus.Rejected);
			application.RejectionReasons.Should().BeEquivalentTo(reasons);
			application.ApprovedTerms.Should().BeNull();

			result.IsApproved.Should().BeFalse();
			result.ApprovedTerms.Should().BeNull();
			result.Reasons.Should().BeEquivalentTo(reasons);
		}

		[Fact]
		public async Task Handle_StartsReviewBeforeFetchingCreditReport()
		{
			var applicant = DomainFixtures.CreateApplicant();
			var application = DomainFixtures.CreateLoanApplication(
				LoanApplicationStatus.Submitted, applicantId: applicant.Id);
			var report = new CreditBureauReport(CreditScore.Of(780), Money.Of(300m, "EUR"));

			LoanApplicationStatus? statusWhenReportFetched = null;

			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);
			_applicantRepository.GetByIdAsync(applicant.Id, Arg.Any<CancellationToken>())
				.Returns(applicant);
			_creditBureauService.GetReportAsync(applicant.Id, Arg.Any<CancellationToken>())
				.Returns(_ =>
				{
					statusWhenReportFetched = application.Status;
					return Task.FromResult(report);
				});
			_underwritingPolicy.Evaluate(
					Arg.Any<Applicant>(), Arg.Any<Money>(), Arg.Any<int>(), Arg.Any<CreditScore>())
				.Returns(DomainFixtures.ApprovedDecision());

			await _handler.Handle(new RunUnderwritingCommand(application.Id), CancellationToken.None);

			statusWhenReportFetched.Should().Be(LoanApplicationStatus.UnderReview);
		}

		[Fact]
		public async Task Handle_UpdatesApplicantObligationsWithBureauDebtBeforeEvaluating()
		{
			var applicant = DomainFixtures.CreateApplicant();
			var application = DomainFixtures.CreateLoanApplication(
				LoanApplicationStatus.Submitted, applicantId: applicant.Id);
			var bureauDebt = Money.Of(300m, "EUR");
			var report = new CreditBureauReport(CreditScore.Of(780), bureauDebt);

			Money? debtWhenEvaluated = null;

			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);
			_applicantRepository.GetByIdAsync(applicant.Id, Arg.Any<CancellationToken>())
				.Returns(applicant);
			_creditBureauService.GetReportAsync(applicant.Id, Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(report));
			_underwritingPolicy.Evaluate(
					Arg.Any<Applicant>(), Arg.Any<Money>(), Arg.Any<int>(), Arg.Any<CreditScore>())
				.Returns(callInfo =>
				{
					debtWhenEvaluated = callInfo.Arg<Applicant>().FinancialObligations.ExistingMonthlyDebt;
					return DomainFixtures.ApprovedDecision();
				});

			await _handler.Handle(new RunUnderwritingCommand(application.Id), CancellationToken.None);

			debtWhenEvaluated.Should().Be(bureauDebt);
			applicant.FinancialObligations.ExistingMonthlyDebt.Should().Be(bureauDebt);
			applicant.FinancialObligations.LastCheckedAtUtc.Should().NotBeNull();
		}

		[Fact]
		public async Task Handle_PassesApplicationAndReportValuesToPolicyAndSavesOnce()
		{
			var (application, applicant, report) = SetUpHappyPath(DomainFixtures.ApprovedDecision());

			await _handler.Handle(new RunUnderwritingCommand(application.Id), CancellationToken.None);

			_underwritingPolicy.Received(1).Evaluate(
				applicant,
				application.RequestedAmount,
				application.RequestedTermMonths,
				report.CreditScore);
			await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WhenLoanApplicationDoesNotExist_ThrowsNotFoundExceptionWithoutMutating()
		{
			var missingId = Guid.NewGuid();
			_loanApplicationRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
				.Returns((LoanApplication?)null);

			var act = () => _handler.Handle(new RunUnderwritingCommand(missingId), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"LoanApplication with key '{missingId}' was not found.");
			await _creditBureauService.DidNotReceive().GetReportAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
			await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
		}

		[Fact]
		public async Task Handle_WhenApplicantDoesNotExist_ThrowsNotFoundExceptionWithoutMutating()
		{
			var application = DomainFixtures.CreateLoanApplication(LoanApplicationStatus.Submitted);
			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);
			_applicantRepository.GetByIdAsync(application.ApplicantId, Arg.Any<CancellationToken>())
				.Returns((Applicant?)null);

			var act = () => _handler.Handle(new RunUnderwritingCommand(application.Id), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"Applicant with key '{application.ApplicantId}' was not found.");
			application.Status.Should().Be(LoanApplicationStatus.Submitted); // review never started
			await _creditBureauService.DidNotReceive().GetReportAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
			await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
		}
	}
}
