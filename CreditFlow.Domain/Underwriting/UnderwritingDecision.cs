
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Domain.Underwriting
{
	/// <summary>
	/// The outcome of running underwriting for a proposed loan. Always carries
	/// Reasons, even on approval, so every decision is explainable — a
	/// regulatory and trust expectation in real lending systems, not just a
	/// nice-to-have.
	/// </summary>
	public sealed class UnderwritingDecision
	{
		public bool IsApproved { get; }
		public LoanTerms? ApprovedTerms { get; }
		public CreditAssessment Assessment { get; }
		public IReadOnlyList<string> Reasons { get; }

		private UnderwritingDecision(
				bool isApproved,
				LoanTerms? approvedTerms,
				CreditAssessment assessment,
				IReadOnlyList<string> reasons)
		{
			IsApproved = isApproved;
			ApprovedTerms = approvedTerms;
			Assessment = assessment;
			Reasons = reasons;
		}

		public static UnderwritingDecision Approve(
			LoanTerms approvedTerms, CreditAssessment assessment, IReadOnlyList<string> reasons)
			=> new(true, approvedTerms, assessment, reasons);

		public static UnderwritingDecision Decline(
			CreditAssessment assessment, IReadOnlyList<string> reasons)
			=> new(false, null, assessment, reasons); 

	}
}
