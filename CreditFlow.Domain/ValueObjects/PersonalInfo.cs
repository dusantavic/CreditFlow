using CreditFlow.Domain.Common;
using CreditFlow.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.ValueObjects
{
	/// <summary>
	/// Identifying personal details of an applicant. Grouped as a single value
	/// object because these fields are always read/validated/replaced together
	/// as one unit, and because it keeps the Applicant entity from being
	/// cluttered with loose primitive fields (name, DOB, national ID).
	/// </summary>
	public sealed class PersonalInfo : ValueObject
	{
		public string FirstName { get; }
		public string LastName { get; }
		public DateOnly DateOfBirth { get; }
		public string NationalId { get; }

		private PersonalInfo(string firstName, string lastName, DateOnly dateOfBirth, string nationalId)
		{
			FirstName = firstName;
			LastName = lastName;
			DateOfBirth = dateOfBirth;
			NationalId = nationalId;
		}

		public static PersonalInfo Of(string firstName, string lastName, DateOnly dateOfBirth, string nationalId)
		{
			if (string.IsNullOrWhiteSpace(firstName))
				throw new BusinessRuleViolationException("First name is required.");

			if (string.IsNullOrWhiteSpace(lastName))
				throw new BusinessRuleViolationException("Last name is required.");

			if (string.IsNullOrWhiteSpace(nationalId))
				throw new BusinessRuleViolationException("National ID is required.");

			var age = CalculateAge(dateOfBirth);
			if (age < 18)
				throw new BusinessRuleViolationException("Applicant must be at least 18 years old.");

			return new PersonalInfo(firstName.Trim(), lastName.Trim(), dateOfBirth, nationalId.Trim());
		}

		public string FullName => $"{FirstName} {LastName}"; 

		private static int CalculateAge(DateOnly dateOfBirth)
		{
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			var age = today.Year - dateOfBirth.Year;
			if (dateOfBirth > today.AddYears(-age)) age--;
			return age; 
		}

		// NationalId is masked here so that logging or ToString() calls never
		// accidentally leak the full value into logs, error messages, or traces.
		public string MaskedNationalId()
			=> NationalId.Length <= 4
				? new string('*', NationalId.Length)
				: new string('*', NationalId.Length - 4) + NationalId[^4..]; 

		protected override IEnumerable<object?> GetEqualityComponents()
		{
			yield return FirstName;
			yield return LastName;
			yield return DateOfBirth;
			yield return NationalId;
		}

		public override string ToString() => $"{FullName} (ID: {MaskedNationalId()})";

	}
}
