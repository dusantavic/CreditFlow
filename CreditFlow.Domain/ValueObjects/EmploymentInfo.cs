using CreditFlow.Domain.Common;
using CreditFlow.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Employment and income details used directly as input to underwriting
	/// (DTI ratio, max loan amount). Grouped as one value object because these
	/// fields are always supplied and re-verified together.
	/// </summary>
	public sealed class EmploymentInfo : ValueObject
	{
		public string EmployerName { get; }
		public Money MonthlyIncome { get; }
		public int YearsEmployed { get; }

		private EmploymentInfo() { } // EF Core

		private EmploymentInfo(string employerName, Money monthlyIncome, int yearsEmployed)
		{
			EmployerName = employerName;
			MonthlyIncome = monthlyIncome;
			YearsEmployed = yearsEmployed;
		}

		public static EmploymentInfo Of(string employerName, Money monthlyIncome, int yearsEmployed)
		{
			if (string.IsNullOrWhiteSpace(employerName))
				throw new BusinessRuleViolationException("Employer name is required.");

			if (monthlyIncome.Amount <= 0)
				throw new BusinessRuleViolationException("Monthly income must be greater than zero.");

			if (yearsEmployed < 0)
				throw new BusinessRuleViolationException("Years employed cannot be negative.");

			return new EmploymentInfo(employerName.Trim(), monthlyIncome, yearsEmployed);
		}

		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return EmployerName;
			yield return MonthlyIncome;
			yield return YearsEmployed;
		}

		public override string ToString()
			=> $"{EmployerName}, {MonthlyIncome}/month income, {YearsEmployed}y employed";
	}
}
