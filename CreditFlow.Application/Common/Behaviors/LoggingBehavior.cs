
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CreditFlow.Application.Common.Behaviors
{
	/// <summary>
	/// Logs the start, completion, and duration of every command/query that
	/// passes through the pipeline. Centralizing this here means individual
	/// handlers never need to add their own logging boilerplate for "a request
	/// started/finished".
	/// </summary>
	public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
		where TRequest : IRequest<TResponse>
	{
		private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

		public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
		{
			_logger = logger;
		}

		public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
		{
			var requestName = typeof(TRequest).Name;
			var stopwatch = Stopwatch.StartNew();

			_logger.LogInformation("Handling {RequestName}", requestName);

			var response = await next();

			stopwatch.Stop();
			_logger.LogInformation(
				"Handled {RequestName} in {ElapsedMilliseonds}ms",
				requestName,
				stopwatch.ElapsedMilliseconds);


			return response; 
		}
	}
}
