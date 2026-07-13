using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreditFlow.Api.IntegrationTests.Infrastructure;
using CreditFlow.Application.Common.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CreditFlow.Api.IntegrationTests.Applicants
{
	public class ApplicantsEndpointTests : IntegrationTestBase
	{
		public ApplicantsEndpointTests(CustomWebApplicationFactory factory) : base(factory)
		{
		}

		[Fact]
		public async Task Register_WithValidData_Returns201AndPersistsApplicant()
		{
			var response = await Client.PostAsJsonAsync("/api/v1/applicants", new
			{
				firstName = "Jane",
				lastName = "Smith",
				dateOfBirth = "1985-03-20",
				nationalId = "9876543210",
				employerName = "Globex",
				monthlyIncome = 6_000m,
				yearsEmployed = 3,
				currency = "EUR"
			});

			response.StatusCode.Should().Be(HttpStatusCode.Created);
			var applicantId = await response.Content.ReadFromJsonAsync<Guid>();
			applicantId.Should().NotBeEmpty();

			// Verify it actually reached the database, not just the response.
			using var scope = Factory.Services.CreateScope();
			var repository = scope.ServiceProvider.GetRequiredService<IApplicantRepository>();
			var persisted = await repository.GetByIdAsync(applicantId);

			persisted.Should().NotBeNull();
			persisted!.PersonalInfo.FullName.Should().Be("Jane Smith");
			persisted.EmploymentInfo.MonthlyIncome.Amount.Should().Be(6_000m);
		}

		[Fact]
		public async Task Register_WithMissingFirstName_Returns400WithErrorForField()
		{
			var response = await Client.PostAsJsonAsync("/api/v1/applicants", new
			{
				firstName = "",
				lastName = "Smith",
				dateOfBirth = "1985-03-20",
				nationalId = "1112223334",
				employerName = "Globex",
				monthlyIncome = 6_000m,
				yearsEmployed = 3,
				currency = "EUR"
			});

			response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

			var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
			problem.GetProperty("status").GetInt32().Should().Be(400);
			problem.GetProperty("errors").TryGetProperty("FirstName", out var firstNameErrors).Should().BeTrue();
			firstNameErrors.EnumerateArray().Should().NotBeEmpty();
		}
	}
}
