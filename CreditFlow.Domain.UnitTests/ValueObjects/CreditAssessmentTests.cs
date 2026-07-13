using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class CreditAssessmentTests
	{
		private static CreditAssessment CreateAssessment(
			int score = 720,
			RiskTier tier = RiskTier.NearPrime,
			decimal dti = 30m,
			IReadOnlyList<string>? reasons = null)
			=> CreditAssessment.Of(
				CreditScore.Of(score),
				tier,
				Percentage.Of(dti),
				reasons ?? new[] { "Approved at risk tier NearPrime." });

		[Fact]
		public void Of_SetsAssessedAtUtcAutomatically()
		{
			var assessment = CreateAssessment();

			assessment.AssessedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
		}

		[Fact]
		public void Of_SetsAllProvidedComponents()
		{
			var reasons = new[] { "reason one", "reason two" };

			var assessment = CreateAssessment(700, RiskTier.Prime, 25.5m, reasons);

			assessment.CreditScore.Should().Be(CreditScore.Of(700));
			assessment.RiskTier.Should().Be(RiskTier.Prime);
			assessment.DebtToIncomeRatio.Should().Be(Percentage.Of(25.5m));
			assessment.Reasons.Should().BeEquivalentTo(reasons);
		}

		// Equality as actually implemented: CreditScore, RiskTier, DebtToIncomeRatio
		// and AssessedAtUtc.Date participate; Reasons does not. Two assessments
		// created on the same UTC day with the same score/tier/DTI are equal even
		// if their reasons differ. (Note: these tests would only be affected if run
		// across a UTC midnight boundary, since AssessedAtUtc.Date participates.)
		[Fact]
		public void Equality_SameScoreTierAndDtiOnSameDay_AreEqual()
		{
			var a = CreateAssessment();
			var b = CreateAssessment();

			a.Should().Be(b);
		}

		[Fact]
		public void Equality_DifferentReasonsOnly_AreStillEqual()
		{
			var a = CreateAssessment(reasons: new[] { "reason A" });
			var b = CreateAssessment(reasons: new[] { "completely different reason" });

			a.Should().Be(b);
		}

		[Fact]
		public void Equality_DifferentCreditScore_AreNotEqual()
		{
			var a = CreateAssessment(score: 700);
			var b = CreateAssessment(score: 701);

			a.Should().NotBe(b);
		}

		[Fact]
		public void Equality_DifferentRiskTier_AreNotEqual()
		{
			var a = CreateAssessment(tier: RiskTier.Prime);
			var b = CreateAssessment(tier: RiskTier.NearPrime);

			a.Should().NotBe(b);
		}

		[Fact]
		public void Equality_DifferentDebtToIncomeRatio_AreNotEqual()
		{
			var a = CreateAssessment(dti: 30m);
			var b = CreateAssessment(dti: 31m);

			a.Should().NotBe(b);
		}
	}
}
