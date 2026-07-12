
namespace CreditFlow.Infrastructure.ExternalServices
{
	/// <summary>
	/// Wire-format shape of CreditBureau.Api's response. Kept separate from
	/// CreditBureauReport (the Application-layer model) because this class
	/// exists only to match the external service's JSON contract — if that
	/// contract changes, this is the only place that needs updating.
	/// </summary>
	internal sealed record CreditBureauReportResponse(int CreditScore, int ExistingMonthlyDebt, string Currency); 
}
