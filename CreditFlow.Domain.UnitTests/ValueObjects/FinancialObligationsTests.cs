using CreditFlow.Domain.ValueObjects;
using FluentAssertions;

namespace CreditFlow.Domain.UnitTests.ValueObjects
{
	public class FinancialObligationsTests
	{
		[Fact]
		public void Of_SetsLastCheckedAtUtcToRecentTimestamp()
		{
			var obligations = FinancialObligations.Of(Money.Of(500m, "EUR"));

			obligations.ExistingMonthlyDebt.Should().Be(Money.Of(500m, "EUR"));
			obligations.LastCheckedAtUtc.Should().NotBeNull();
			obligations.LastCheckedAtUtc!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
		}

		[Fact]
		public void None_ProducesZeroDebtWithNullLastCheckedAtUtc()
		{
			var obligations = FinancialObligations.None("EUR");

			obligations.ExistingMonthlyDebt.Should().Be(Money.Zero("EUR"));
			obligations.LastCheckedAtUtc.Should().BeNull();
		}

		[Fact]
		public void IsStale_WhenNeverChecked_ReturnsTrue()
		{
			var obligations = FinancialObligations.None("EUR");

			obligations.IsStale(TimeSpan.FromDays(365)).Should().BeTrue();
		}

		[Fact]
		public void IsStale_WhenOlderThanMaxAge_ReturnsTrue()
		{
			var obligations = FinancialObligations.Of(Money.Of(500m, "EUR"));

			Thread.Sleep(20); // let real time pass beyond the 1ms max age below

			obligations.IsStale(TimeSpan.FromMilliseconds(1)).Should().BeTrue();
		}

		[Fact]
		public void IsStale_WhenWithinMaxAge_ReturnsFalse()
		{
			var obligations = FinancialObligations.Of(Money.Of(500m, "EUR"));

			obligations.IsStale(TimeSpan.FromHours(1)).Should().BeFalse();
		}

		[Fact]
		public void Equality_SameDebtDifferentCheckTimes_AreEqual()
		{
			// LastCheckedAtUtc is intentionally excluded from equality.
			var checkedNow = FinancialObligations.Of(Money.Of(500m, "EUR"));
			var neverChecked = FinancialObligations.None("EUR");
			var sameDebtCheckedNow = FinancialObligations.Of(Money.Of(0m, "EUR"));

			neverChecked.Should().Be(sameDebtCheckedNow);
			checkedNow.Should().NotBe(neverChecked); // different debt amounts
		}
	}
}
