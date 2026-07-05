
using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Domain.Underwriting
{
	/// <summary>
	/// Default underwriting policy implementing the bank's core lending rules:
	/// DTI-based auto-decline, credit-score risk tiering, tier-based max loan
	/// amount and interest rate. Thresholds are kept as named constants (not
	/// injected configuration) deliberately — see the class remarks for why.
	/// </summary>
	public sealed class UnderwritingPolicy : IUnderwritingPolicy
	{
		private readonly UnderwritingPolicyOptions _options; 

		public UnderwritingPolicy(UnderwritingPolicyOptions options)
		{
			_options = options; 
		}

		public UnderwritingDecision Evaluate(
			Applicant applicant, 
			Money requestedAmount, 
			int requestedTermMonths, 
			CreditScore creditScore)
		{
			var reasons = new List<string>();
			var riskTier = DetermineRiskTier(creditScore); 

			if (riskTier == RiskTier.Declined)
			{
				reasons.Add(
					$"Credit score {creditScore} is below the minimum acceptable score pf {_options.MinAcceptableCreditScore}.");
				var declinedAssessment = CreditAssessment.Of(creditScore, riskTier, Percentage.Of(0), reasons);
				return UnderwritingDecision.Decline(declinedAssessment, reasons); 
			}


			var tierSettings = GetSettingsFor(riskTier);
			var interestRate = InterestRate.OfAnnual(tierSettings.AnnualInterestRate);
			var maxLoanAmount = applicant.EmploymentInfo.MonthlyIncome.Multiply(tierSettings.IncomeMultiplier);

			var cappedAmount = requestedAmount.IsGreaterThan(maxLoanAmount) ? maxLoanAmount : requestedAmount; 
			if (requestedAmount.IsGreaterThan(maxLoanAmount))
			{
				reasons.Add(
					$"Requested amount {requestedAmount} exceeds the maximum of {maxLoanAmount} for risk tier {riskTier}; capped to maximum.");
			}

			var proposedTerms = LoanTerms.Of(cappedAmount, interestRate, requestedTermMonths);
			var dtiRatio = applicant.CalculateDebtToIncomeRatio(proposedTerms.MonthlyPayment); 

			if (dtiRatio.IsGreaterThan(Percentage.Of(_options.MaxAllowedDtiPercentage)))
			{
				reasons.Add(
					$"Debt-to-income ratio of {dtiRatio} exceeds the maximum allowed {_options.MaxAllowedDtiPercentage}%.");
				var declinedAssessment = CreditAssessment.Of(creditScore, riskTier, dtiRatio, reasons);
				return UnderwritingDecision.Decline(declinedAssessment, reasons); 
			}

			reasons.Add($"Approved at risk tier {riskTier} with DTI {dtiRatio}.");
			var approvedAssessment = CreditAssessment.Of(creditScore, riskTier, dtiRatio, reasons);
			return UnderwritingDecision.Approve(proposedTerms, approvedAssessment, reasons); 
		}

		private RiskTier DetermineRiskTier(CreditScore creditScore)
		{
			if (creditScore.Value >= _options.PrimeMinScore) return RiskTier.Prime;
			if (creditScore.Value >= _options.NearPrimeMinScore) return RiskTier.NearPrime;
			if (creditScore.Value >= _options.MinAcceptableCreditScore) return RiskTier.SubPrime;
			return RiskTier.Declined; 
		}

		private RiskTierSettings GetSettingsFor(RiskTier riskTier)
		{
			return riskTier switch 
			{
				RiskTier.Prime => _options.Prime, 
				RiskTier.NearPrime => _options.NearPrime, 
				RiskTier.SubPrime => _options.SubPrime, 
				_ => throw new ArgumentOutOfRangeException(
				nameof(riskTier), "Declined tier has no applicable tier settings.")
			};
		}
	}
}
