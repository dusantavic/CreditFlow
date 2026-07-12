using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Application.Common.Models;
using CreditFlow.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace CreditFlow.Infrastructure.ExternalServices
{
	/// <summary>
	/// Calls the separately-hosted CreditBureau.Api over HTTP to obtain a
	/// credit report. Retry/timeout/circuit-breaker behavior for transient
	/// network failures is configured on the HttpClient itself (DependencyInjection.AddInfrastructure), not here —
	/// this class only knows how to make the call and map the response, not how to be
	/// resilient about it.
	/// </summary>
	public sealed class HttpCreditBureauService : ICreditBureauService
	{
		private readonly HttpClient _httpClient; 

		public HttpCreditBureauService(HttpClient httpClient)
		{
			_httpClient = httpClient; 
		}

		public async Task<CreditBureauReport> GetReportAsync(Guid applicantId, CancellationToken cancellationToken = default)
		{
			var response = await _httpClient.GetFromJsonAsync<CreditBureauReportResponse>(
				$"/api/credit-report/{applicantId}", cancellationToken); 

			if (response is null)
			{
				throw new InvalidOperationException(
					$"Credit bureau returned an empty response for applicant {applicantId}.");
			}

			var creditScore = CreditScore.Of(response.CreditScore);
			var existingDebt = Money.Of(response.ExistingMonthlyDebt, response.Currency);

			return new CreditBureauReport(creditScore, existingDebt); 
		}


	}
}
