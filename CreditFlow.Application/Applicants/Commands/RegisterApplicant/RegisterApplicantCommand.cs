
using MediatR;

namespace CreditFlow.Application.Applicants.Commands.RegisterApplicant
{
	/// <summary>
	/// Registers a new applicant in the system. This is a prerequisite step —
	/// an applicant must exist before any loan application can reference them,
	/// which also allows the same applicant to submit multiple applications
	/// over time (see GetApplicantLoanHistoryQuery).
	/// </summary>
	public sealed record RegisterApplicantCommand(
		string FirstName,
		string LastName,
		DateOnly DateOfBirth,
		string NationalId,
		string EmployerName,
		decimal MonthlyIncome,
		int YearsEmployed,
		string Currency
		) : IRequest<Guid>; 
}
