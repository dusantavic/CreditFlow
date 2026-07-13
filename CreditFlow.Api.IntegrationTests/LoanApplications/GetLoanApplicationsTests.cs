using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreditFlow.Api.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CreditFlow.Api.IntegrationTests.LoanApplications
{
	public class GetLoanApplicationsTests : IntegrationTestBase
	{
		public GetLoanApplicationsTests(CustomWebApplicationFactory factory) : base(factory)
		{
		}

		/// <summary>
		/// Seeds one applicant with one Submitted and one Cancelled application.
		/// Every query below also filters by this applicant's id so the results
		/// are isolated from data created by other tests sharing the database.
		/// </summary>
		private async Task<(Guid ApplicantId, Guid SubmittedId, Guid CancelledId)> SeedTwoApplicationsAsync()
		{
			var applicantId = await RegisterApplicantAsync();
			var submittedId = await SubmitLoanApplicationAsync(applicantId, requestedAmount: 10_000m);
			var cancelledId = await SubmitLoanApplicationAsync(applicantId, requestedAmount: 5_000m);

			var cancelResponse = await Client.PostAsync(
				$"/api/v1/loan-applications/{cancelledId}/cancel", null);
			cancelResponse.EnsureSuccessStatusCode();

			return (applicantId, submittedId, cancelledId);
		}

		[Fact]
		public async Task GetAll_FilteredByApplicant_ReturnsPagedResultShapeWithAllTheirApplications()
		{
			var (applicantId, _, _) = await SeedTwoApplicationsAsync();

			var result = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications?applicantId={applicantId}");

			result.GetProperty("totalCount").GetInt32().Should().Be(2);
			result.GetProperty("pageNumber").GetInt32().Should().Be(1);
			result.GetProperty("pageSize").GetInt32().Should().Be(20);
			result.GetProperty("totalPages").GetInt32().Should().Be(1);
			result.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse();
			result.GetProperty("hasNextPage").GetBoolean().Should().BeFalse();
			result.GetProperty("items").EnumerateArray().Should().HaveCount(2);
		}

		[Fact]
		public async Task GetAll_FilteredByStatus_NarrowsResultsToThatStatus()
		{
			var (applicantId, submittedId, cancelledId) = await SeedTwoApplicationsAsync();

			var submittedOnly = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications?applicantId={applicantId}&status=Submitted");

			var items = submittedOnly.GetProperty("items").EnumerateArray().ToList();
			items.Should().ContainSingle();
			items[0].GetProperty("id").GetGuid().Should().Be(submittedId);
			items[0].GetProperty("status").GetString().Should().Be("Submitted");

			var cancelledOnly = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications?applicantId={applicantId}&status=Cancelled");

			var cancelledItems = cancelledOnly.GetProperty("items").EnumerateArray().ToList();
			cancelledItems.Should().ContainSingle();
			cancelledItems[0].GetProperty("id").GetGuid().Should().Be(cancelledId);
		}

		[Fact]
		public async Task GetAll_WithDateRangeFilter_NarrowsResultsBySubmissionDate()
		{
			var (applicantId, _, _) = await SeedTwoApplicationsAsync();

			var yesterday = DateTime.UtcNow.AddDays(-1).ToString("O");
			var tomorrow = DateTime.UtcNow.AddDays(1).ToString("O");

			var inRange = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications?applicantId={applicantId}&submittedFromUtc={yesterday}&submittedToUtc={tomorrow}");
			inRange.GetProperty("totalCount").GetInt32().Should().Be(2);

			var futureOnly = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications?applicantId={applicantId}&submittedFromUtc={tomorrow}");
			futureOnly.GetProperty("totalCount").GetInt32().Should().Be(0);
			futureOnly.GetProperty("items").EnumerateArray().Should().BeEmpty();
		}

		[Fact]
		public async Task GetAll_WithPageSizeOne_PagesThroughResults()
		{
			var (applicantId, _, _) = await SeedTwoApplicationsAsync();

			var firstPage = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications?applicantId={applicantId}&pageNumber=1&pageSize=1");

			firstPage.GetProperty("totalCount").GetInt32().Should().Be(2);
			firstPage.GetProperty("totalPages").GetInt32().Should().Be(2);
			firstPage.GetProperty("items").EnumerateArray().Should().HaveCount(1);
			firstPage.GetProperty("hasNextPage").GetBoolean().Should().BeTrue();

			var secondPage = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications?applicantId={applicantId}&pageNumber=2&pageSize=1");

			secondPage.GetProperty("items").EnumerateArray().Should().HaveCount(1);
			secondPage.GetProperty("hasPreviousPage").GetBoolean().Should().BeTrue();
			secondPage.GetProperty("hasNextPage").GetBoolean().Should().BeFalse();

			var firstId = firstPage.GetProperty("items")[0].GetProperty("id").GetGuid();
			var secondId = secondPage.GetProperty("items")[0].GetProperty("id").GetGuid();
			firstId.Should().NotBe(secondId);
		}

		[Fact]
		public async Task GetAll_WithInvalidPageSize_Returns400()
		{
			var response = await Client.GetAsync("/api/v1/loan-applications?pageSize=0");

			response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		}
	}
}
