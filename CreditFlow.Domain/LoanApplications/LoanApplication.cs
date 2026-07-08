
using CreditFlow.Domain.Common;
using CreditFlow.Domain.Exceptions;
using CreditFlow.Domain.LoanApplications.Events;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Domain.LoanApplications
{
	/// <summary>
	/// Aggregate root for the loan origination process. Owns and enforces its
	/// own state machine — no external code (application handlers, controllers)
	/// is allowed to set Status directly; every transition goes through a
	/// named method that validates the current state first.
	/// </summary>
	public sealed class LoanApplication : AggregateRoot<Guid>
	{
		public Guid ApplicantId { get; private set; }
		public Money RequestedAmount { get; private set; }
		public int RequestedTermMonths { get; private set; }
		public string Purpose { get; private set; }
		public LoanApplicationStatus Status { get; private set; }
		public DateTime SubmittedAtUtc { get; private set; }
		public CreditAssessment? CreditAssessment { get; private set; }
		public LoanTerms? ApprovedTerms { get; private set; }
		public IReadOnlyList<string>? RejectionReasons { get; private set; }
		
		private LoanApplication(
			Guid id,
			Guid applicantId,
			Money requestedAmount,
			int requestedTermMonths,
			string purpose) : base(id)
		{
			ApplicantId = applicantId;
			RequestedAmount = requestedAmount;
			RequestedTermMonths = requestedTermMonths;
			Purpose = purpose;
			Status = LoanApplicationStatus.Draft;
		}

		public static LoanApplication CreateDraft(
			Guid applicantId, 
			Money requestedAmount, 
			int requestedTermMonths, 
			string purpose)
		{
			if (requestedAmount.Amount <= 0)
				throw new BusinessRuleViolationException("Requested amount must be greater than zero.");

			if (requestedTermMonths <= 0)
				throw new BusinessRuleViolationException("Requested term must be at least 1 month.");

			if (string.IsNullOrWhiteSpace(purpose))
				throw new BusinessRuleViolationException("Loan purpose is required.");

			return new LoanApplication(Guid.CreateVersion7(), applicantId, requestedAmount, requestedTermMonths, purpose.Trim()); 
		}

		public void Submit()
		{
			EnsureStatusIs(LoanApplicationStatus.Draft, nameof(Submit));

			Status = LoanApplicationStatus.Submitted;
			SubmittedAtUtc = DateTime.UtcNow;

			RaiseDomainEvent(new LoanApplicationSubmitted(Id, ApplicantId)); 
		}

		public void StartReview()
		{
			EnsureStatusIs(LoanApplicationStatus.Submitted, nameof(StartReview));

			Status = LoanApplicationStatus.UnderReview; 
		}

		/// <summary>
		/// Records the outcome of underwriting. This does NOT decide
		/// approve/reject itself — that decision is made by UnderwritingPolicy
		/// (a domain service) and passed in here. The aggregate's job is only
		/// to enforce that this can happen exactly once, from the correct
		/// state, and to transition accordingly.
		/// </summary>
		public void ApplyUnderwritingDecision(UnderwritingDecision decision)
		{
			EnsureStatusIs(LoanApplicationStatus.UnderReview, nameof(ApplyUnderwritingDecision));

			CreditAssessment = decision.Assessment; 

			if (decision.IsApproved)
			{
				ApprovedTerms = decision.ApprovedTerms;
				Status = LoanApplicationStatus.Approved;
				RaiseDomainEvent(new LoanApplicationApproved(Id, ApplicantId)); 
			}
			else
			{
				RejectionReasons = decision.Reasons;
				Status = LoanApplicationStatus.Rejected;
				RaiseDomainEvent(new LoanApplicationRejected(Id, ApplicantId, decision.Reasons)); 
			}

			RaiseDomainEvent(new CreditAssessmentCompleted(Id, decision.IsApproved)); 
		}

		/// <summary>
		/// Manual override path (e.g. a human underwriter approving despite an
		/// automated decline, or approving with adjusted terms). Kept separate
		/// from ApplyUnderwritingDecision so the automated path and the manual
		/// override path are each explicit in the aggregate's public API rather
		/// than overloading a single method with an "isManual" flag.
		/// </summary>
		/// 
		public void Approve(LoanTerms terms)
		{
			EnsureStatusIs(LoanApplicationStatus.UnderReview, nameof(Approve));

			ApprovedTerms = terms;
			Status = LoanApplicationStatus.Approved;

			RaiseDomainEvent(new LoanApplicationApproved(Id, ApplicantId)); 
		}

		public void Reject(IReadOnlyList<string> reasons)
		{
			EnsureStatusIs(LoanApplicationStatus.UnderReview, nameof(Reject));

			if (reasons is null || reasons.Count == 0)
				throw new BusinessRuleViolationException("At least one rejection reason is required.");

			RejectionReasons = reasons;
			Status = LoanApplicationStatus.Rejected;

			RaiseDomainEvent(new LoanApplicationRejected(Id, ApplicantId, reasons)); 
		}

		public void Disburse()
		{
			EnsureStatusIs(LoanApplicationStatus.Approved, nameof(Disburse));

			Status = LoanApplicationStatus.Disbursed;

			RaiseDomainEvent(new LoanDisbursed(Id, ApplicantId)); 
		}

		public void Cancel()
		{
			if (Status != LoanApplicationStatus.Draft && Status != LoanApplicationStatus.Submitted)
				throw new InvalidLoanStateTransitionException(Status.ToString(), nameof(Cancel));

			Status = LoanApplicationStatus.Cancelled;
		}

		private void EnsureStatusIs(LoanApplicationStatus requiredStatus, string attemptedAction)
		{
			if (Status != requiredStatus)
				throw new InvalidLoanStateTransitionException(Status.ToString(), attemptedAction);
		}
	}
}
