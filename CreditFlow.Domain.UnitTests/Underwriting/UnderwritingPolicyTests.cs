using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.Underwriting
{
	public class UnderwritingPolicyTests
	{
		// Self-contained fixture values — deliberately NOT read from appsettings.json,
		// so these tests keep passing even if configuration defaults change later.
		private static UnderwritingPolicyOptions DefaultOptions() => new()
		{
			MinAcceptableCreditScore = 580,
			NearPrimeMinScore = 650,
			PrimeMinScore = 750,
			MaxAllowedDtiPercentage = 45m,
			Prime = new RiskTierSettings { AnnualInterestRate = 5.5m, IncomeMultiplier = 10m },
			NearPrime = new RiskTierSettings { AnnualInterestRate = 8.9m, IncomeMultiplier = 6m },
			SubPrime = new RiskTierSettings { AnnualInterestRate = 14.9m, IncomeMultiplier = 3m }
		};

		private static Applicant CreateApplicant(decimal monthlyIncome = 5_000m, decimal existingMonthlyDebt = 0m)
			=> Applicant.Register(
				PersonalInfo.Of("John", "Doe", new DateOnly(1990, 5, 15), "1234567890"),
				EmploymentInfo.Of("Acme Corp", Money.Of(monthlyIncome, "EUR"), 5),
				existingMonthlyDebt == 0m
					? FinancialObligations.None("EUR")
					: FinancialObligations.Of(Money.Of(existingMonthlyDebt, "EUR")));

		private static UnderwritingDecision Evaluate(
			int creditScore,
			decimal requestedAmount = 10_000m,
			int termMonths = 24,
			Applicant? applicant = null,
			UnderwritingPolicyOptions? options = null)
		{
			var policy = new UnderwritingPolicy(options ?? DefaultOptions());
			return policy.Evaluate(
				applicant ?? CreateApplicant(),
				Money.Of(requestedAmount, "EUR"),
				termMonths,
				CreditScore.Of(creditScore));
		}

		[Fact]
		public void Evaluate_WithScoreBelowMinimum_DeclinesWithCreditScoreReason()
		{
			var decision = Evaluate(creditScore: 579);

			decision.IsApproved.Should().BeFalse();
			decision.ApprovedTerms.Should().BeNull();
			decision.Assessment.RiskTier.Should().Be(RiskTier.Declined);
			decision.Assessment.DebtToIncomeRatio.Should().Be(Percentage.Of(0m));
			decision.Reasons.Should().ContainSingle()
				.Which.Should().Match("*Credit score 579 is below the minimum acceptable score of 580*");
		}

		// Tier boundaries as implemented (all inclusive lower bounds, checked with >=):
		// score >= 750 -> Prime; >= 650 -> NearPrime; >= 580 -> SubPrime; else Declined.
		[Theory]
		[InlineData(580, RiskTier.SubPrime)]  // exactly MinAcceptableCreditScore
		[InlineData(649, RiskTier.SubPrime)]  // one below NearPrimeMinScore
		[InlineData(650, RiskTier.NearPrime)] // exactly NearPrimeMinScore
		[InlineData(749, RiskTier.NearPrime)] // one below PrimeMinScore
		[InlineData(750, RiskTier.Prime)]     // exactly PrimeMinScore
		[InlineData(850, RiskTier.Prime)]
		public void Evaluate_AssignsRiskTierByScoreBand(int score, RiskTier expectedTier)
		{
			var decision = Evaluate(creditScore: score);

			decision.Assessment.RiskTier.Should().Be(expectedTier);
			decision.IsApproved.Should().BeTrue(); // fixture applicant comfortably passes DTI in every tier
		}

		[Theory]
		[InlineData(600, 14.9)]  // SubPrime
		[InlineData(700, 8.9)]   // NearPrime
		[InlineData(800, 5.5)]   // Prime
		public void Evaluate_AppliesTierSpecificInterestRate(int score, decimal expectedAnnualRate)
		{
			var decision = Evaluate(creditScore: score);

			decision.ApprovedTerms.Should().NotBeNull();
			decision.ApprovedTerms!.InterestRate.Should().Be(InterestRate.OfAnnual(expectedAnnualRate));
		}

		[Theory]
		[InlineData(600, 3)]   // SubPrime: max = income x 3
		[InlineData(700, 6)]   // NearPrime: max = income x 6
		[InlineData(800, 10)]  // Prime: max = income x 10
		public void Evaluate_WithRequestAboveTierMaximum_ApprovesCappedToIncomeMultiplier(int score, decimal incomeMultiplier)
		{
			// Income 5000 EUR; request 100,000 exceeds every tier's maximum, term kept
			// short-ish so the capped payment still passes the 45% DTI check for Prime/NearPrime,
			// but SubPrime (15,000 @ 14.9% / 60mo => ~356/mo, DTI ~7%) passes easily too.
			var decision = Evaluate(creditScore: score, requestedAmount: 100_000m, termMonths: 60);

			var expectedCap = Money.Of(5_000m * incomeMultiplier, "EUR");

			decision.IsApproved.Should().BeTrue();
			decision.ApprovedTerms!.PrincipalAmount.Should().Be(expectedCap);
			decision.Reasons.Should().Contain(r => r.Contains("capped to maximum"));
		}

		[Fact]
		public void Evaluate_WithDtiAboveMaximum_DeclinesDespiteQualifyingScore()
		{
			// Prime score, but income of 1000 EUR against a 10,000 EUR / 12-month request:
			// monthly payment ~858 EUR => DTI ~86% > 45% => declined on DTI, not score.
			var applicant = CreateApplicant(monthlyIncome: 1_000m);

			var decision = Evaluate(creditScore: 800, requestedAmount: 10_000m, termMonths: 12, applicant: applicant);

			decision.IsApproved.Should().BeFalse();
			decision.ApprovedTerms.Should().BeNull();
			decision.Assessment.RiskTier.Should().Be(RiskTier.Prime); // tier was assigned; the DTI check declined it
			decision.Reasons.Should().Contain(r => r.Contains("Debt-to-income ratio") && r.Contains("exceeds the maximum allowed 45%"));
		}

		[Fact]
		public void Evaluate_WithDtiExactlyAtMaximum_Approves()
		{
			// The implementation declines only when DTI is strictly greater than
			// MaxAllowedDtiPercentage (IsGreaterThan), so exactly 45.00% must pass.
			// Zero-interest SubPrime tier makes the payment exact: 5400 / 12 = 450,
			// and 450 / 1000 income = 45.00% DTI.
			var options = new UnderwritingPolicyOptions
			{
				MinAcceptableCreditScore = 580,
				NearPrimeMinScore = 650,
				PrimeMinScore = 750,
				MaxAllowedDtiPercentage = 45m,
				Prime = new RiskTierSettings { AnnualInterestRate = 5.5m, IncomeMultiplier = 10m },
				NearPrime = new RiskTierSettings { AnnualInterestRate = 8.9m, IncomeMultiplier = 6m },
				SubPrime = new RiskTierSettings { AnnualInterestRate = 0m, IncomeMultiplier = 10m }
			};
			var applicant = CreateApplicant(monthlyIncome: 1_000m);

			var decision = Evaluate(creditScore: 600, requestedAmount: 5_400m, termMonths: 12, applicant, options);

			decision.Assessment.DebtToIncomeRatio.Should().Be(Percentage.Of(45m));
			decision.IsApproved.Should().BeTrue();
		}

		[Fact]
		public void Evaluate_WithDtiJustAboveMaximum_Declines()
		{
			// Same zero-interest setup, one cent more per month: 5401.20 / 12 = 450.10,
			// giving DTI 45.01% — strictly greater than 45%, so declined.
			var options = new UnderwritingPolicyOptions
			{
				MinAcceptableCreditScore = 580,
				NearPrimeMinScore = 650,
				PrimeMinScore = 750,
				MaxAllowedDtiPercentage = 45m,
				Prime = new RiskTierSettings { AnnualInterestRate = 5.5m, IncomeMultiplier = 10m },
				NearPrime = new RiskTierSettings { AnnualInterestRate = 8.9m, IncomeMultiplier = 6m },
				SubPrime = new RiskTierSettings { AnnualInterestRate = 0m, IncomeMultiplier = 10m }
			};
			var applicant = CreateApplicant(monthlyIncome: 1_000m);

			var decision = Evaluate(creditScore: 600, requestedAmount: 5_401.20m, termMonths: 12, applicant, options);

			decision.Assessment.DebtToIncomeRatio.Should().Be(Percentage.Of(45.01m));
			decision.IsApproved.Should().BeFalse();
		}

		[Fact]
		public void Evaluate_WithFullyQualifyingRequest_ApprovesWithExpectedTermsAndReasons()
		{
			// Prime applicant, request well within limits: 20,000 EUR over 24 months
			// at the Prime rate of 5.5%; max amount is 5000 x 10 = 50,000, no capping.
			var decision = Evaluate(creditScore: 780, requestedAmount: 20_000m, termMonths: 24);

			decision.IsApproved.Should().BeTrue();
			decision.ApprovedTerms.Should().Be(
				LoanTerms.Of(Money.Of(20_000m, "EUR"), InterestRate.OfAnnual(5.5m), 24));
			decision.Assessment.CreditScore.Should().Be(CreditScore.Of(780));
			decision.Assessment.RiskTier.Should().Be(RiskTier.Prime);
			decision.Reasons.Should().NotBeEmpty();
			decision.Reasons.Should().ContainSingle()
				.Which.Should().Match("Approved at risk tier Prime with DTI*");
		}
	}
}
