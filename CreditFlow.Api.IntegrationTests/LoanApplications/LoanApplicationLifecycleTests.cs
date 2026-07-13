using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreditFlow.Api.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CreditFlow.Api.IntegrationTests.LoanApplications
{
	public class LoanApplicationLifecycleTests : IntegrationTestBase
	{
		public LoanApplicationLifecycleTests(CustomWebApplicationFactory factory) : base(factory)
		{
		}

		[Fact]
		public async Task HappyPath_RegisterSubmitUnderwriteDisburse_EndsInDisbursedStatus()
		{
			var applicantId = await RegisterApplicantAsync(monthlyIncome: 5_000m);
			var applicationId = await SubmitLoanApplicationAsync(applicantId, requestedAmount: 10_000m);

			// Prime-tier score with no existing debt: guaranteed approval for this request.
			CreditBureau.CreditScore = 780;
			CreditBureau.ExistingMonthlyDebt = 0m;

			var underwriteResponse = await Client.PostAsync(
				$"/api/v1/loan-applications/{applicationId}/underwrite", null);
			underwriteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

			var result = await underwriteResponse.Content.ReadFromJsonAsync<JsonElement>();
			result.GetProperty("isApproved").GetBoolean().Should().BeTrue();
			result.GetProperty("riskTier").GetString().Should().Be("Prime");
			result.GetProperty("approvedTerms").ValueKind.Should().Be(JsonValueKind.Object);

			var disburseResponse = await Client.PostAsync(
				$"/api/v1/loan-applications/{applicationId}/disburse", null);
			disburseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

			var details = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications/{applicationId}");
			details.GetProperty("status").GetString().Should().Be("Disbursed");
			details.GetProperty("approvedTerms").ValueKind.Should().Be(JsonValueKind.Object);
			details.GetProperty("creditAssessment").GetProperty("creditScore").GetInt32().Should().Be(780);
		}

		[Fact]
		public async Task RejectionPath_UnderwriteWithLowScore_EndsInRejectedStatusWithReasons()
		{
			var applicantId = await RegisterApplicantAsync(monthlyIncome: 5_000m);
			var applicationId = await SubmitLoanApplicationAsync(applicantId, requestedAmount: 10_000m);

			CreditBureau.CreditScore = 500; // below any acceptable tier
			CreditBureau.ExistingMonthlyDebt = 0m;

			var underwriteResponse = await Client.PostAsync(
				$"/api/v1/loan-applications/{applicationId}/underwrite", null);
			underwriteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

			var result = await underwriteResponse.Content.ReadFromJsonAsync<JsonElement>();
			result.GetProperty("isApproved").GetBoolean().Should().BeFalse();
			result.GetProperty("riskTier").GetString().Should().Be("Declined");

			var details = await Client.GetFromJsonAsync<JsonElement>(
				$"/api/v1/loan-applications/{applicationId}");
			details.GetProperty("status").GetString().Should().Be("Rejected");
			var rejectionReasons = details.GetProperty("rejectionReasons").EnumerateArray().ToList();
			rejectionReasons.Should().NotBeEmpty();
			rejectionReasons[0].GetString().Should().Contain("Credit score");
		}

		[Fact]
		public async Task Disburse_WhenApplicationNotApproved_Returns409Conflict()
		{
			var applicantId = await RegisterApplicantAsync();
			var applicationId = await SubmitLoanApplicationAsync(applicantId);

			var response = await Client.PostAsync(
				$"/api/v1/loan-applications/{applicationId}/disburse", null);

			response.StatusCode.Should().Be(HttpStatusCode.Conflict);
			var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
			problem.GetProperty("detail").GetString().Should()
				.Contain("Cannot perform 'Disburse'").And.Contain("Submitted");
		}

		[Fact]
		public async Task GetById_WithNonexistentId_Returns404NotFound()
		{
			var response = await Client.GetAsync($"/api/v1/loan-applications/{Guid.NewGuid()}");

			response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		}
	}
}
