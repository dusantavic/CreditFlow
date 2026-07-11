

using System.Collections;
using CreditFlow.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CreditFlow.Infrastructure.Persistence
{
	/// <summary>
	/// Thin wrapper around DbContext.SaveChangesAsync. Exists so the
	/// Application layer depends only on IUnitOfWork, never on DbContext or
	/// EF Core directly — swapping persistence technology later would mean
	/// changing this implementation, not every handler that calls it.
	/// </summary>
	public sealed class UnitOfWork : IUnitOfWork
	{
		private readonly CreditFlowDbContext _dbContext;

		public UnitOfWork(CreditFlowDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			DetachOrphanedOwnedEntries();
			return _dbContext.SaveChangesAsync(cancellationToken);
		}

		/// <summary>
		/// Our owned types (Money, InterestRate, LoanTerms, FinancialObligations,
		/// etc.) are immutable value objects: replacing one always means
		/// assigning a brand-new instance to the owner's navigation property
		/// (e.g. LoanApplication.ApprovedTerms, Applicant.FinancialObligations)
		/// rather than mutating the tracked instance in place. When that owned
		/// type itself owns a further nested owned type, EF Core's change
		/// detector can't reconcile the old (still-attached) nested instance
		/// with the new one — both share the same key (the aggregate root's id)
		/// — and throws "cannot be tracked because another instance with the
		/// same key is already being tracked" instead of just replacing it.
		/// Since owned entities carry no identity of their own, the fix is to
		/// walk the graph from every non-owned root, note which owned instances
		/// are still actually reachable, and detach any tracked owned entry that
		/// isn't (i.e. was left behind by a reassignment) before EF inspects the
		/// graph in SaveChanges.
		/// </summary>
		private void DetachOrphanedOwnedEntries()
		{
			var autoDetectChangesEnabled = _dbContext.ChangeTracker.AutoDetectChangesEnabled;
			_dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

			try
			{
				var reachable = new HashSet<object>(ReferenceEqualityComparer.Instance);

				var roots = _dbContext.ChangeTracker.Entries()
					.Where(e => !e.Metadata.IsOwned())
					.Select(e => (e.Entity, e.Metadata))
					.ToList();

				foreach (var (entity, entityType) in roots)
					CollectReachableOwned(entity, entityType, reachable);

				foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
				{
					if (entry.Metadata.IsOwned() && !reachable.Contains(entry.Entity))
						entry.State = EntityState.Detached;
				}
			}
			finally
			{
				_dbContext.ChangeTracker.AutoDetectChangesEnabled = autoDetectChangesEnabled;
			}
		}

		private static void CollectReachableOwned(object? entity, IEntityType entityType, HashSet<object> reachable)
		{
			if (entity is null)
				return;

			foreach (var navigation in entityType.GetNavigations())
			{
				if (!navigation.TargetEntityType.IsOwned())
					continue;

				var value = navigation.PropertyInfo?.GetValue(entity) ?? navigation.FieldInfo?.GetValue(entity);
				if (value is null)
					continue;

				if (navigation.IsCollection)
				{
					foreach (var item in (IEnumerable)value)
					{
						if (item is null || !reachable.Add(item))
							continue;

						CollectReachableOwned(item, navigation.TargetEntityType, reachable);
					}
				}
				else if (reachable.Add(value))
				{
					CollectReachableOwned(value, navigation.TargetEntityType, reachable);
				}
			}
		}
	}
}
