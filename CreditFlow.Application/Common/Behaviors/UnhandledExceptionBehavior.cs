using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreditFlow.Application.Common.Behaviors
{
	/// <summary>
	/// Catches any exception that isn't already an expected, handled type
	/// (domain exceptions and ValidationException are allowed to propagate as-is,
	/// since the API's middleware maps those to specific HTTP status codes).
	/// This behavior exists purely to guarantee that a truly unexpected failure
	/// is logged with full context before it propagates further, rather than
	/// silently reaching the middleware with no trace of what request caused it.
	/// </summary>
	public sealed class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
		where TRequest : IRequest<TResponse>
	{
		private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

		public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
		{
			_logger = logger;
		}

		public async Task<TResponse> Handle(
			TRequest request, 
			RequestHandlerDelegate<TResponse> next, 
			CancellationToken cancellationToken)
		{
			try
			{
				return await next(); 
			}
			catch (Exception ex) when (ex is not Common.Exceptions.RequestValidationException
				and not Common.Exceptions.NotFoundException)
			{
				var requestName = typeof(TRequest).Name;
				_logger.LogError(ex, "Unhandled exception for request {RequestName}", requestName);
				throw;
			}
		}
	}
}
