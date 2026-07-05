namespace CreditFlow.Domain.LoanApplications
{
	/// <summary>
	/// The lifecycle states of a loan application. Transitions between these
	/// are enforced exclusively by LoanApplication itself — no other code sets
	/// this value directly.
	/// </summary>
	public enum LoanApplicationStatus
	{
		Draft = 0, 
		Submitted = 1, 
		UnderReview = 2, 
		Approved = 3, 
		Rejected = 4, 
		Disbursed = 5, 
		Cancelled = 6
	}
}
