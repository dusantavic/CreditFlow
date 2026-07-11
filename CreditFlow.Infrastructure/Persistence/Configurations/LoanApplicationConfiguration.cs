using CreditFlow.Domain.LoanApplications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditFlow.Infrastructure.Persistence.Configurations
{
	public sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
	{
		public void Configure(EntityTypeBuilder<LoanApplication> builder)
		{
			builder.ToTable("LoanApplications");

			builder.HasKey(la => la.Id);
			builder.Property(la => la.Id).ValueGeneratedNever();

			builder.Property(la => la.ApplicantId).IsRequired();
			builder.Property(la => la.RequestedTermMonths).IsRequired();
			builder.Property(la => la.Purpose).HasMaxLength(500).IsRequired();
			builder.Property(la => la.SubmittedAtUtc).IsRequired();

			// Status is a domain enum, stored as its string name rather than
			// the underlying int — makes the database human-readable when
			// inspected directly (e.g. "Approved" instead of "3"), at a small
			// storage cost that's irrelevant at this scale.
			builder.Property(la => la.Status)
				.HasConversion<string>()
				.HasMaxLength(20)
				.IsRequired();

			builder.OwnsOne(la => la.RequestedAmount, money =>
			{
				money.Property(m => m.Amount).HasColumnName("RequestedAmount").HasColumnType("decimal(18,2)").IsRequired();
				money.Property(m => m.Currency).HasColumnName("RequestedAmountCurrency").HasMaxLength(3).IsRequired();
			});

			builder.OwnsOne(la => la.CreditAssessment, assessment =>
			{
				assessment.OwnsOne(a => a.CreditScore, score =>
				{
					score.Property(s => s.Value).HasColumnName("CreditScore");
				});

				assessment.Property(a => a.RiskTier)
				.HasConversion<string>()
				.HasColumnName("RiskTier")
				.HasMaxLength(20);

				assessment.OwnsOne(a => a.DebtToIncomeRatio, dti =>
				{
					dti.Property(p => p.Value).HasColumnName("DebtToIncomeRatioPercentage").HasColumnType("decimal(5,2)");
				});

				assessment.Property(a => a.AssessedAtUtc).HasColumnName("AssessedAtUtc");

				// Reasons is a List<string> - stored as a delimited string via
				// a value converter; a normalized child table would be overkill.
				assessment.Property(a => a.Reasons)
					.HasColumnName("AssessmentReasons")
					.HasConversion(
						reasons => string.Join('|', reasons),
						serialized => serialized.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
					.Metadata.SetValueComparer(StringListComparer.Instance);
			});

			builder.OwnsOne(la => la.ApprovedTerms, terms =>
			{
				terms.OwnsOne(t => t.PrincipalAmount, money =>
				{
					money.Property(m => m.Amount).HasColumnName("ApprovedPrincipalAmount").HasColumnType("decimal(18,2)");
					money.Property(m => m.Currency).HasColumnName("ApprovedPrincipalCurrency").HasMaxLength(3);
				});

				terms.OwnsOne(t => t.InterestRate, rate =>
				{
					rate.OwnsOne(r => r.AnnualRate, percentage =>
					{
						percentage.Property(p => p.Value).HasColumnName("AnnualInterestRatePercentage").HasColumnType("decimal(5,2)");
					});
				});

				terms.Property(t => t.TermMonths).HasColumnName("ApprovedTermMonths");

				terms.OwnsOne(t => t.MonthlyPayment, money =>
				{
					money.Property(m => m.Amount).HasColumnName("MonthlyPaymentAmount").HasColumnType("decimal(18,2)");
					money.Property(m => m.Currency).HasColumnName("MonthlyPaymentCurrency").HasMaxLength(3);
				});
			});

			builder.Property(la => la.RejectionReasons) 
				.HasColumnName("RejectionReasons")
				.HasConversion(
					reasons => reasons == null ? null : string.Join('|', reasons),
					serialized => serialized == null ? null : serialized.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
				.Metadata.SetValueComparer(StringListComparer.Instance);

			// Domain events are NOT persisted — they exist only in memory until
			// the SaveChanges interceptor dispatches and clears them.
			builder.Ignore(la => la.DomainEvents);
		}
	}
}
