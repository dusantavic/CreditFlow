using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.LoanApplications;
using CreditFlow.Domain.LoanApplications.Events;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.LoanApplications
{
	public class LoanApplicationTests
	{
		private static readonly Guid ApplicantId = Guid.NewGuid();

		private static LoanApplication CreateDraft()
			=> LoanApplication.CreateDraft(ApplicantId, Money.Of(10_000m, "EUR"), 12, "Car purchase");

		private static LoanTerms SampleTerms()
			=> LoanTerms.Of(Money.Of(10_000m, "EUR"), InterestRate.OfAnnual(5.5m), 12);

		private static CreditAssessment SampleAssessment(RiskTier tier = RiskTier.Prime)
			=> CreditAssessment.Of(CreditScore.Of(780), tier, Percentage.Of(20m), new[] { "reason" });

		/// <summary>
		/// Drives a fresh application to the requested status through the
		/// aggregate's own public transitions, so tests never bypass the state machine.
		/// </summary>
		private static LoanApplication CreateInStatus(LoanApplicationStatus status)
		{
			var application = CreateDraft();

			switch (status)
			{
				case LoanApplicationStatus.Draft:
					break;
				case LoanApplicationStatus.Submitted:
					application.Submit();
					break;
				case LoanApplicationStatus.UnderReview:
					application.Submit();
					application.StartReview();
					break;
				case LoanApplicationStatus.Approved:
					application.Submit();
					application.StartReview();
					application.Approve(SampleTerms());
					break;
				case LoanApplicationStatus.Rejected:
					application.Submit();
					application.StartReview();
					application.Reject(new[] { "rejected in test setup" });
					break;
				case LoanApplicationStatus.Disbursed:
					application.Submit();
					application.StartReview();
					application.Approve(SampleTerms());
					application.Disburse();
					break;
				case LoanApplicationStatus.Cancelled:
					application.Cancel();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status));
			}

			application.ClearDomainEvents(); // setup transitions shouldn't pollute event assertions
			return application;
		}

		private static void AssertInvalidTransition(Action act, LoanApplicationStatus currentStatus, string attemptedAction)
			=> act.Should().Throw<InvalidLoanStateTransitionException>()
				.WithMessage($"Cannot perform '{attemptedAction}' while the loan application is in status '{currentStatus}'.");

		// ---------- CreateDraft ----------

		[Fact]
		public void CreateDraft_WithValidData_StartsInDraftStatus()
		{
			var application = LoanApplication.CreateDraft(ApplicantId, Money.Of(10_000m, "EUR"), 12, "  Car purchase ");

			application.Id.Should().NotBeEmpty();
			application.ApplicantId.Should().Be(ApplicantId);
			application.RequestedAmount.Should().Be(Money.Of(10_000m, "EUR"));
			application.RequestedTermMonths.Should().Be(12);
			application.Purpose.Should().Be("Car purchase"); // trimmed
			application.Status.Should().Be(LoanApplicationStatus.Draft);
			application.DomainEvents.Should().BeEmpty();
		}

		[Fact]
		public void CreateDraft_WithZeroRequestedAmount_ThrowsBusinessRuleViolationException()
		{
			var act = () => LoanApplication.CreateDraft(ApplicantId, Money.Of(0m, "EUR"), 12, "Car purchase");

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*Requested amount must be greater than zero*");
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-6)]
		public void CreateDraft_WithZeroOrNegativeTermMonths_ThrowsBusinessRuleViolationException(int invalidTerm)
		{
			var act = () => LoanApplication.CreateDraft(ApplicantId, Money.Of(10_000m, "EUR"), invalidTerm, "Car purchase");

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*term must be at least 1 month*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void CreateDraft_WithMissingPurpose_ThrowsBusinessRuleViolationException(string? purpose)
		{
			var act = () => LoanApplication.CreateDraft(ApplicantId, Money.Of(10_000m, "EUR"), 12, purpose!);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*purpose is required*");
		}

		// ---------- Submit ----------

		[Fact]
		public void Submit_FromDraft_MovesToSubmittedAndRaisesEvent()
		{
			var application = CreateDraft();

			application.Submit();

			application.Status.Should().Be(LoanApplicationStatus.Submitted);
			application.SubmittedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
			application.DomainEvents.Should().ContainSingle()
				.Which.Should().BeOfType<LoanApplicationSubmitted>()
				.Which.LoanApplicationId.Should().Be(application.Id);
		}

		[Theory]
		[InlineData(LoanApplicationStatus.Submitted)]
		[InlineData(LoanApplicationStatus.UnderReview)]
		[InlineData(LoanApplicationStatus.Approved)]
		[InlineData(LoanApplicationStatus.Rejected)]
		[InlineData(LoanApplicationStatus.Disbursed)]
		[InlineData(LoanApplicationStatus.Cancelled)]
		public void Submit_FromNonDraftStatus_ThrowsInvalidLoanStateTransitionException(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);

			AssertInvalidTransition(() => application.Submit(), status, "Submit");
		}

		// ---------- StartReview ----------

		[Fact]
		public void StartReview_FromSubmitted_MovesToUnderReview()
		{
			var application = CreateInStatus(LoanApplicationStatus.Submitted);

			application.StartReview();

			application.Status.Should().Be(LoanApplicationStatus.UnderReview);
		}

		[Theory]
		[InlineData(LoanApplicationStatus.Draft)]
		[InlineData(LoanApplicationStatus.UnderReview)]
		[InlineData(LoanApplicationStatus.Approved)]
		[InlineData(LoanApplicationStatus.Rejected)]
		[InlineData(LoanApplicationStatus.Disbursed)]
		[InlineData(LoanApplicationStatus.Cancelled)]
		public void StartReview_FromNonSubmittedStatus_ThrowsInvalidLoanStateTransitionException(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);

			AssertInvalidTransition(() => application.StartReview(), status, "StartReview");
		}

		// ---------- ApplyUnderwritingDecision ----------

		[Fact]
		public void ApplyUnderwritingDecision_WithApprovedDecision_MovesToApprovedAndRaisesEvents()
		{
			var application = CreateInStatus(LoanApplicationStatus.UnderReview);
			var terms = SampleTerms();
			var assessment = SampleAssessment();
			var decision = UnderwritingDecision.Approve(terms, assessment, new[] { "approved" });

			application.ApplyUnderwritingDecision(decision);

			application.Status.Should().Be(LoanApplicationStatus.Approved);
			application.ApprovedTerms.Should().Be(terms);
			application.CreditAssessment.Should().Be(assessment);
			application.RejectionReasons.Should().BeNull();
			application.DomainEvents.OfType<LoanApplicationApproved>().Should().ContainSingle();
			application.DomainEvents.OfType<CreditAssessmentCompleted>().Should().ContainSingle()
				.Which.WasApproved.Should().BeTrue();
		}

		[Fact]
		public void ApplyUnderwritingDecision_WithDeclinedDecision_MovesToRejectedAndRaisesEvents()
		{
			var application = CreateInStatus(LoanApplicationStatus.UnderReview);
			var assessment = SampleAssessment(RiskTier.Declined);
			var reasons = new[] { "Credit score too low." };
			var decision = UnderwritingDecision.Decline(assessment, reasons);

			application.ApplyUnderwritingDecision(decision);

			application.Status.Should().Be(LoanApplicationStatus.Rejected);
			application.RejectionReasons.Should().BeEquivalentTo(reasons);
			application.CreditAssessment.Should().Be(assessment);
			application.ApprovedTerms.Should().BeNull();
			application.DomainEvents.OfType<LoanApplicationRejected>().Should().ContainSingle()
				.Which.Reasons.Should().BeEquivalentTo(reasons);
			application.DomainEvents.OfType<CreditAssessmentCompleted>().Should().ContainSingle()
				.Which.WasApproved.Should().BeFalse();
		}

		[Theory]
		[InlineData(LoanApplicationStatus.Draft)]
		[InlineData(LoanApplicationStatus.Submitted)]
		[InlineData(LoanApplicationStatus.Approved)]
		[InlineData(LoanApplicationStatus.Rejected)]
		[InlineData(LoanApplicationStatus.Disbursed)]
		[InlineData(LoanApplicationStatus.Cancelled)]
		public void ApplyUnderwritingDecision_FromNonUnderReviewStatus_ThrowsInvalidLoanStateTransitionException(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);
			var decision = UnderwritingDecision.Approve(SampleTerms(), SampleAssessment(), new[] { "approved" });

			AssertInvalidTransition(
				() => application.ApplyUnderwritingDecision(decision), status, "ApplyUnderwritingDecision");
		}

		// ---------- Approve (manual override) ----------

		[Fact]
		public void Approve_FromUnderReview_SetsTermsAndRaisesEvent()
		{
			var application = CreateInStatus(LoanApplicationStatus.UnderReview);
			var terms = SampleTerms();

			application.Approve(terms);

			application.Status.Should().Be(LoanApplicationStatus.Approved);
			application.ApprovedTerms.Should().Be(terms);
			application.DomainEvents.Should().ContainSingle()
				.Which.Should().BeOfType<LoanApplicationApproved>();
		}

		[Theory]
		[InlineData(LoanApplicationStatus.Draft)]
		[InlineData(LoanApplicationStatus.Submitted)]
		[InlineData(LoanApplicationStatus.Approved)]
		[InlineData(LoanApplicationStatus.Rejected)]
		[InlineData(LoanApplicationStatus.Disbursed)]
		[InlineData(LoanApplicationStatus.Cancelled)]
		public void Approve_FromNonUnderReviewStatus_ThrowsInvalidLoanStateTransitionException(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);

			AssertInvalidTransition(() => application.Approve(SampleTerms()), status, "Approve");
		}

		// ---------- Reject ----------

		[Fact]
		public void Reject_FromUnderReview_SetsReasonsAndRaisesEvent()
		{
			var application = CreateInStatus(LoanApplicationStatus.UnderReview);
			var reasons = new[] { "Suspected fraud." };

			application.Reject(reasons);

			application.Status.Should().Be(LoanApplicationStatus.Rejected);
			application.RejectionReasons.Should().BeEquivalentTo(reasons);
			application.DomainEvents.Should().ContainSingle()
				.Which.Should().BeOfType<LoanApplicationRejected>()
				.Which.Reasons.Should().BeEquivalentTo(reasons);
		}

		[Fact]
		public void Reject_WithNullReasons_ThrowsBusinessRuleViolationException()
		{
			var application = CreateInStatus(LoanApplicationStatus.UnderReview);

			var act = () => application.Reject(null!);

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*At least one rejection reason is required*");
		}

		[Fact]
		public void Reject_WithEmptyReasons_ThrowsBusinessRuleViolationException()
		{
			var application = CreateInStatus(LoanApplicationStatus.UnderReview);

			var act = () => application.Reject(Array.Empty<string>());

			act.Should().Throw<BusinessRuleViolationException>()
				.WithMessage("*At least one rejection reason is required*");
		}

		[Theory]
		[InlineData(LoanApplicationStatus.Draft)]
		[InlineData(LoanApplicationStatus.Submitted)]
		[InlineData(LoanApplicationStatus.Approved)]
		[InlineData(LoanApplicationStatus.Rejected)]
		[InlineData(LoanApplicationStatus.Disbursed)]
		[InlineData(LoanApplicationStatus.Cancelled)]
		public void Reject_FromNonUnderReviewStatus_ThrowsInvalidLoanStateTransitionException(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);

			AssertInvalidTransition(() => application.Reject(new[] { "reason" }), status, "Reject");
		}

		// ---------- Disburse ----------

		[Fact]
		public void Disburse_FromApproved_MovesToDisbursedAndRaisesEvent()
		{
			var application = CreateInStatus(LoanApplicationStatus.Approved);

			application.Disburse();

			application.Status.Should().Be(LoanApplicationStatus.Disbursed);
			application.DomainEvents.Should().ContainSingle()
				.Which.Should().BeOfType<LoanDisbursed>();
		}

		[Theory]
		[InlineData(LoanApplicationStatus.Draft)]
		[InlineData(LoanApplicationStatus.Submitted)]
		[InlineData(LoanApplicationStatus.UnderReview)]
		[InlineData(LoanApplicationStatus.Rejected)]
		[InlineData(LoanApplicationStatus.Disbursed)]
		[InlineData(LoanApplicationStatus.Cancelled)]
		public void Disburse_FromNonApprovedStatus_ThrowsInvalidLoanStateTransitionException(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);

			AssertInvalidTransition(() => application.Disburse(), status, "Disburse");
		}

		// ---------- Cancel ----------

		[Theory]
		[InlineData(LoanApplicationStatus.Draft)]
		[InlineData(LoanApplicationStatus.Submitted)]
		public void Cancel_FromDraftOrSubmitted_MovesToCancelled(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);

			application.Cancel();

			application.Status.Should().Be(LoanApplicationStatus.Cancelled);
		}

		[Theory]
		[InlineData(LoanApplicationStatus.UnderReview)]
		[InlineData(LoanApplicationStatus.Approved)]
		[InlineData(LoanApplicationStatus.Rejected)]
		[InlineData(LoanApplicationStatus.Disbursed)]
		[InlineData(LoanApplicationStatus.Cancelled)]
		public void Cancel_FromNonCancellableStatus_ThrowsInvalidLoanStateTransitionException(LoanApplicationStatus status)
		{
			var application = CreateInStatus(status);

			AssertInvalidTransition(() => application.Cancel(), status, "Cancel");
		}
	}
}
