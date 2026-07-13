using System.Net.Http.Json;
using CreditFlow.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CreditFlow.Api.IntegrationTests.Infrastructure
{
	[Collection(IntegrationTestCollection.Name)]
	public abstract class IntegrationTestBase
	{
		protected CustomWebApplicationFactory Factory { get; }
		protected HttpClient Client { get; }
		protected FakeCreditBureauService CreditBureau => Factory.CreditBureau;

		protected IntegrationTestBase(CustomWebApplicationFactory factory)
		{
			Factory = factory;
			Client = factory.CreateClient();
		}

		protected async Task<Guid> RegisterApplicantAsync(decimal monthlyIncome = 5_000m, string currency = "EUR")
		{
			var response = await Client.PostAsJsonAsync("/api/v1/applicants", new
			{
				firstName = "John",
				lastName = "Doe",
				dateOfBirth = "1990-05-15",
				nationalId = Guid.NewGuid().ToString("N")[..10], // unique per call
				employerName = "Acme Corp",
				monthlyIncome,
				yearsEmployed = 5,
				currency
			});

			response.EnsureSuccessStatusCode();
			return await response.Content.ReadFromJsonAsync<Guid>();
		}

		protected async Task<Guid> SubmitLoanApplicationAsync(
			Guid applicantId,
			decimal requestedAmount = 10_000m,
			int requestedTermMonths = 12,
			string currency = "EUR")
		{
			var response = await Client.PostAsJsonAsync("/api/v1/loan-applications", new
			{
				applicantId,
				requestedAmount,
				currency,
				requestedTermMonths,
				purpose = "Car purchase"
			});

			response.EnsureSuccessStatusCode();
			return await response.Content.ReadFromJsonAsync<Guid>();
		}

		/// <summary>
		/// Drives an application to UnderReview directly through the domain +
		/// repositories. Needed because the public API only passes through
		/// UnderReview transiently inside the underwrite endpoint, so there is
		/// no HTTP-only way to park an application in that state for testing
		/// the manual approve/reject endpoints' success paths.
		/// </summary>
		protected async Task StartReviewAsync(Guid loanApplicationId)
		{
			using var scope = Factory.Services.CreateScope();
			var repository = scope.ServiceProvider.GetRequiredService<ILoanApplicationRepository>();
			var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

			var application = await repository.GetByIdAsync(loanApplicationId)
				?? throw new InvalidOperationException($"Test setup: application {loanApplicationId} not found.");

			application.StartReview();
			await unitOfWork.SaveChangesAsync();
		}
	}
}
