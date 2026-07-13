using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Models;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Api.IntegrationTests.Infrastructure
{
	/// <summary>
	/// In-memory stand-in for the external CreditBureau.Api. Tests set the
	/// score/debt they need before triggering underwriting, so approval and
	/// rejection outcomes are deterministic and never depend on the real
	/// bureau service being reachable.
	/// </summary>
	public sealed class FakeCreditBureauService : ICreditBureauService
	{
		public int CreditScore { get; set; } = 780;
		public decimal ExistingMonthlyDebt { get; set; } = 0m;
		public string Currency { get; set; } = "EUR";

		public Task<CreditBureauReport> GetReportAsync(Guid applicantId, CancellationToken cancellationToken = default)
			=> Task.FromResult(new CreditBureauReport(
				Domain.ValueObjects.CreditScore.Of(CreditScore),
				Money.Of(ExistingMonthlyDebt, Currency)));
	}
}
