using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.Common
{
	public abstract class ValueObject : IEquatable<ValueObject>
	{
		//comparation by the value of the components (structural equality), not by identity or a reference 

		protected abstract IEnumerable<object?> GetEqualityComponents();

		public bool Equals(ValueObject? other)
		{
			if (other is null || other.GetType != GetType()) return false;
			return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as ValueObject);
		}
		public override int GetHashCode()
		{
			return GetEqualityComponents()
				.Select(c => c?.GetHashCode() ?? 0)
				.Aggregate((x, y) => x ^ y);
		}

		public static bool operator ==(ValueObject? left, ValueObject? right)
			=> left is null ? right is null : left.Equals(right);

		public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);

	}
}
