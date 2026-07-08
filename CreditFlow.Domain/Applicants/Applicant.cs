
using CreditFlow.Domain.Common;
using CreditFlow.Domain.ValueObjects;

namespace CreditFlow.Domain.Applicants
{
	/// <summary>
	/// A person applying for a loan. Holds identifying, employment, and debt
	/// information used as direct input to underwriting. Employment and
	/// financial obligations can change over time (job change, debt paid off)
	/// while the applicant's identity stays the same — which is exactly why
	/// this is an Entity and not a Value Object.
	/// </summary>
	public sealed class Applicant : Entity<Guid>
	{
		public PersonalInfo PersonalInfo { get; private set; }
		public EmploymentInfo EmploymentInfo { get; private set; }
		public FinancialObligations FinancialObligations { get; private set; }
		public DateTime RegisteredAtUtc { get; private set; }

		private Applicant(
			Guid id,
			PersonalInfo personalInfo,
			EmploymentInfo employmentInfo,
			FinancialObligations financialObligations,
			DateTime registeredAtUtc) : base(id)
		{
			PersonalInfo = personalInfo;
			EmploymentInfo = employmentInfo;
			FinancialObligations = financialObligations;
			RegisteredAtUtc = registeredAtUtc;
		}

		public static Applicant Register(
		PersonalInfo personalInfo,
		EmploymentInfo employmentInfo,
		FinancialObligations financialObligations)
		{
			employmentInfo.MonthlyIncome.EnsureSameCurrencyAs(
				financialObligations.ExistingMonthlyDebt,
				"Monthly income and existing debt must be expressed in the same currency");

			return new Applicant(
				Guid.CreateVersion7(), //timely sorted like autoincrement
				personalInfo,
				employmentInfo,
				financialObligations,
				DateTime.UtcNow); 
		}

		// Employment and debt can legitimately change after registration (new
		// job, paid-off loan) — the applicant's identity (Id) doesn't change
		// when these are updated, which is the whole point of modeling this as
		// an Entity rather than treating the applicant as an immutable snapshot.
		public void UpdateEmploymentInfo(EmploymentInfo newEmploymentInfo)
		{
			newEmploymentInfo.MonthlyIncome.EnsureSameCurrencyAs(
				FinancialObligations.ExistingMonthlyDebt,
				"Monthly income and existing debt must be expressed in the same currency");
			EmploymentInfo = newEmploymentInfo;
		}

		public void UpdateFinancialObligations(FinancialObligations newFinancialObligations)
		{
			EmploymentInfo.MonthlyIncome.EnsureSameCurrencyAs(
				newFinancialObligations.ExistingMonthlyDebt,
				"Monthly income and existing debt must be expressed in the same currency");
			FinancialObligations = newFinancialObligations;
		}

		/// <summary>
		/// Calculates the debt-to-income ratio that WOULD result if the given
		/// proposed monthly payment were added on top of existing debt. Lives
		/// here (not in UnderwritingPolicy) because it only combines data the
		/// Applicant already owns and guarantees is currency-consistent — the
		/// policy decides what to DO with the ratio, this just computes it.
		/// </summary>
		public Percentage CalculateDebtToIncomeRatio(Money proposedMonthlyPayment)
		{
			EmploymentInfo.MonthlyIncome.EnsureSameCurrencyAs(
				 proposedMonthlyPayment,
				 "Proposed monthly payment must be in the same currency as the applicant's income");

			var totalMonthlyDept = FinancialObligations.ExistingMonthlyDebt.Add(proposedMonthlyPayment);

			var ratioValue = totalMonthlyDept.Amount / EmploymentInfo.MonthlyIncome.Amount * 100m;
			var cappedRatio = Math.Min(ratioValue, 100m);

			return Percentage.Of(cappedRatio); 
		}

	}
}
