using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreditFlow.Api.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CreditFlow.Api.IntegrationTests.LoanApplications
{
	public class ManualDecisionEndpointTests : IntegrationTestBase
	{
		public ManualDecisionEndpointTests(CustomWebApplicationFactory factory) : base(factory)
		{
		}

		private static object ApproveBody() => new
		{
			approvedAmount = 8_000m,
			currency = "EUR",
			annualInterestRatePercentage = 5.5m,
			termMonths = 24
		};

		[Fact]
		public async Task Approve_WhenApplicationUnderReview_Returns200AndApproves()
		{
			var applicantId = await RegisterApplicantAsync();
			var applicationId = await SubmitLoanApplicationAsync(applicantId);
			await StartReviewAsync(applicationId);

			var response = await Client.PostAsJsonAsync(
				$"/api/v1/loan-applications/{applicationId}/approve", ApproveBody());

			response.StatusCode.Should().Be(HttpStatusCode.OK);

			var details = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications/{applicationId}");
			details.GetProperty("status").GetString().Should().Be("Approved");
			var terms = details.GetProperty("approvedTerms");
			terms.GetProperty("principalAmount").GetDecimal().Should().Be(8_000m);
			terms.GetProperty("annualInterestRatePercentage").GetDecimal().Should().Be(5.5m);
			terms.GetProperty("termMonths").GetInt32().Should().Be(24);
		}

		[Fact]
		public async Task Approve_WhenApplicationOnlySubmitted_Returns409Conflict()
		{
			var applicantId = await RegisterApplicantAsync();
			var applicationId = await SubmitLoanApplicationAsync(applicantId);

			var response = await Client.PostAsJsonAsync(
				$"/api/v1/loan-applications/{applicationId}/approve", ApproveBody());

			response.StatusCode.Should().Be(HttpStatusCode.Conflict);
			var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
			problem.GetProperty("detail").GetString().Should()
				.Contain("Cannot perform 'Approve'").And.Contain("Submitted");
		}

		[Fact]
		public async Task Reject_WhenApplicationUnderReview_Returns200AndRejectsWithReasons()
		{
			var applicantId = await RegisterApplicantAsync();
			var applicationId = await SubmitLoanApplicationAsync(applicantId);
			await StartReviewAsync(applicationId);

			var response = await Client.PostAsJsonAsync(
				$"/api/v1/loan-applications/{applicationId}/reject",
				new { reasons = new[] { "Suspected fraud." } });

			response.StatusCode.Should().Be(HttpStatusCode.OK);

			var details = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications/{applicationId}");
			details.GetProperty("status").GetString().Should().Be("Rejected");
			details.GetProperty("rejectionReasons").EnumerateArray()
				.Select(r => r.GetString())
				.Should().ContainSingle()
				.Which.Should().Be("Suspected fraud.");
		}

		[Fact]
		public async Task Reject_WhenApplicationOnlySubmitted_Returns409Conflict()
		{
			var applicantId = await RegisterApplicantAsync();
			var applicationId = await SubmitLoanApplicationAsync(applicantId);

			var response = await Client.PostAsJsonAsync(
				$"/api/v1/loan-applications/{applicationId}/reject",
				new { reasons = new[] { "Suspected fraud." } });

			response.StatusCode.Should().Be(HttpStatusCode.Conflict);
			var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
			problem.GetProperty("detail").GetString().Should()
				.Contain("Cannot perform 'Reject'").And.Contain("Submitted");
		}
	}
}
