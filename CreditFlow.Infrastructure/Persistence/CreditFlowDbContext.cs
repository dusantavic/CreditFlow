using CreditFlow.Domain.Applicants;
using CreditFlow.Domain.LoanApplications;
using Microsoft.EntityFrameworkCore;
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
			base.OnModelCreating(modelBuilder); 
		}
	}
}
