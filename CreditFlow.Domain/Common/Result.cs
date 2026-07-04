using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Domain.Common
{
	public class Result
	{
		public bool IsSuccess { get; }
		public bool IsFailure => !IsSuccess; 
		public IReadOnlyList<string> Errors { get; }

		protected Result(bool isSuccess, IReadOnlyList<string> errors)
		{
			if (isSuccess && errors.Count > 0)
			{
				throw new InvalidOperationException("A successful result cannot contain errors.");
			}
			if (!isSuccess && errors.Count == 0)
			{
				throw new InvalidOperationException("A failed result must contain at least one error."); 
			}

			IsSuccess = isSuccess;
			Errors = errors; 
		}

		public static Result Success() => new(true, Array.Empty<string>());
		public static Result Failure(params string[] errors) => new(false, errors);
		public static Result Failure(IReadOnlyList<string> errors) => new(false, errors);

		public static Result<T> Success<T>(T value) => new(value, true, Array.Empty<string>());
		public static Result<T> Failure<T>(params string[] errors) => new(default, false, errors);
		public static Result<T> Failure<T>(IReadOnlyList<string> errors) => new(default, false, errors);
	}

	public sealed class Result<T> : Result
	{
		private readonly T? _value;

		public T Value => IsSuccess
			? _value!
			: throw new InvalidOperationException("Cannot access the value of a failed result.");

		internal Result(T? value, bool isSuccess, IReadOnlyList<string> errors)
			: base(isSuccess, errors)
		{
			_value = value; 
		}
	}
}
