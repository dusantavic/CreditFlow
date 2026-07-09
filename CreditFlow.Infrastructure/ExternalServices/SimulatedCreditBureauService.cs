using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Models;
using CreditFlow.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Infrastructure.ExternalServices
{
	//@dusan temporary
	public sealed class SimulatedCreditBureauService : ICreditBureauService
	{
		public Task<CreditBureauReport> GetReportAsync(Guid applicantId, CancellationToken cancellationToken = default)
		{
			var seed = applicantId.GetHashCode();
			var random = new Random(seed);

			var score = CreditScore.Of(random.Next(300, 851));
			var existingDebt = Money.Of(random.Next(0, 800), "EUR");

			return Task.FromResult(new CreditBureauReport(score, existingDebt));
		}
	}
}
