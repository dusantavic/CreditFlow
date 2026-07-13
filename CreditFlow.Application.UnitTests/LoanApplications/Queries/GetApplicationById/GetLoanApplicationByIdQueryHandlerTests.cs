using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.LoanApplications.Queries.GetApplicationById;
using CreditFlow.Application.UnitTests.TestHelpers;
using CreditFlow.Domain.LoanApplications;
using FluentAssertions;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.LoanApplications.Queries.GetApplicationById
{
	public class GetLoanApplicationByIdQueryHandlerTests
	{
		private readonly ILoanApplicationRepository _loanApplicationRepository = Substitute.For<ILoanApplicationRepository>();
		private readonly GetLoanApplicationByIdQueryHandler _handler;

		public GetLoanApplicationByIdQueryHandlerTests()
		{
			_handler = new GetLoanApplicationByIdQueryHandler(_loanApplicationRepository);
		}

		[Fact]
		public async Task Handle_WithSubmittedApplication_MapsScalarFieldsAndLeavesDecisionFieldsNull()
		{
			var application = DomainFixtures.CreateLoanApplication(LoanApplicationStatus.Submitted);
			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);

			var result = await _handler.Handle(
				new GetLoanApplicationByIdQuery(application.Id), CancellationToken.None);

			result.Id.Should().Be(application.Id);
			result.ApplicantId.Should().Be(application.ApplicantId);
			result.RequestedAmount.Should().Be(application.RequestedAmount.Amount);
			result.Currency.Should().Be(application.RequestedAmount.Currency);
			result.RequestedTermMonths.Should().Be(application.RequestedTermMonths);
			result.Purpose.Should().Be(application.Purpose);
			result.Status.Should().Be("Submitted");
			result.SubmittedAtUtc.Should().Be(application.SubmittedAtUtc);
			result.CreditAssessment.Should().BeNull();
			result.ApprovedTerms.Should().BeNull();
			result.RejectionReasons.Should().BeNull();
		}

		[Fact]
		public async Task Handle_WithUnderwrittenApplication_MapsAssessmentAndTermsDtos()
		{
			var application = DomainFixtures.CreateLoanApplication(LoanApplicationStatus.UnderReview);
			var terms = DomainFixtures.SampleTerms();
			var decision = DomainFixtures.ApprovedDecision(terms);
			application.ApplyUnderwritingDecision(decision);

			_loanApplicationRepository.GetByIdAsync(application.Id, Arg.Any<CancellationToken>())
				.Returns(application);

			var result = await _handler.Handle(
				new GetLoanApplicationByIdQuery(application.Id), CancellationToken.None);

			result.Status.Should().Be("Approved");
			result.CreditAssessment.Should().NotBeNull();
			result.CreditAssessment!.CreditScore.Should().Be(decision.Assessment.CreditScore.Value);
			result.CreditAssessment.RiskTier.Should().Be(decision.Assessment.RiskTier.ToString());
			result.CreditAssessment.DebtToIncomeRatioPercentage.Should().Be(decision.Assessment.DebtToIncomeRatio.Value);
			result.ApprovedTerms.Should().NotBeNull();
			result.ApprovedTerms!.PrincipalAmount.Should().Be(terms.PrincipalAmount.Amount);
			result.ApprovedTerms.MonthlyPayment.Should().Be(terms.MonthlyPayment.Amount);
		}

		[Fact]
		public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundException()
		{
			var missingId = Guid.NewGuid();
			_loanApplicationRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
				.Returns((LoanApplication?)null);

			var act = () => _handler.Handle(new GetLoanApplicationByIdQuery(missingId), CancellationToken.None);

			await act.Should().ThrowAsync<NotFoundException>()
				.WithMessage($"LoanApplication with key '{missingId}' was not found.");
		}
	}
}
