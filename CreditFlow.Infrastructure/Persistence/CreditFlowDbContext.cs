using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.LoanApplications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Infrastructure.Persistence
{
	public sealed class CreditFlowDbContext : DbContext
	{

		public CreditFlowDbContext(DbContextOptions<CreditFlowDbContext> options) : base(options) { }

		public DbSet<Applicant> Applicants => Set<Applicant>();
		public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// Applies every IEntityTypeConfiguration<T> found in this assembly —
			// avoids having to manually register each configuration class here
			// as the model grows.
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(CreditFlowDbContext).Assembly);


			// Npgsql requires DateTime values mapped to `timestamptz` to have
			// Kind = Utc explicitly set. Every DateTime in this domain is already
			// produced via DateTime.UtcNow, but this converter is a safety net
			// that normalizes Kind on the way in/out regardless of how a value
			// was constructed, avoiding a class of runtime errors that's easy to
			// hit accidentally as the codebase grows.
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				foreach (var property in entityType.GetProperties())
				{
					if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
					{
						property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
							v => DateTime.SpecifyKind(v, DateTimeKind.Utc), //saving
							v => DateTime.SpecifyKind(v, DateTimeKind.Utc))); //reading
					}
				}
			}

			base.OnModelCreating(modelBuilder); 
		}
	}
}
