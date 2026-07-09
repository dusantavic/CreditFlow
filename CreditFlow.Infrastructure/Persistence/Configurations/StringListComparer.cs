using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CreditFlow.Infrastructure.Persistence.Configurations
{
	/// <summary>
	/// Tells EF Core's change tracker how to compare two List<string>
	/// instances by content rather than by reference, which is required for
	/// any collection property mapped through HasConversion.
	/// </summary>
	public sealed class StringListComparer : ValueComparer<IReadOnlyList<string>>
	{
		public static readonly StringListComparer Instance = new(); 

		public StringListComparer() : base( 
			(a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()), 
			list => list == null ? 0 : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())), 
			list => list.ToList())
		{
		}
	}
}
