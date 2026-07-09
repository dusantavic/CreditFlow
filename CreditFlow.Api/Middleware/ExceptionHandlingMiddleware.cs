using CreditFlow.Application.Common.Exceptions;
using CreditFlow.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace CreditFlow.Api.Middleware
{
	/// <summary>
	/// Central place where every exception that escapes a handler gets mapped
	/// to an HTTP status code and a consistent JSON error shape. Handlers and
	/// controllers never need their own try/catch for these cases.
	/// </summary>
	public sealed class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ExceptionHandlingMiddleware> _logger;

		public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
		{
			_next = next;
			_logger = logger; 
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context); 
			}
			catch (Exception exception)
			{
				await HandleExceptionAsync(context, exception); 
			}
		}

		private async Task HandleExceptionAsync(HttpContext context, Exception exception)
		{ 
			var (statusCode, title) = MapException(exception);

			var problemDetails = new
			{
				type = $"https://httpstatuses.com/{(int)statusCode}",
				title,
				status = (int)statusCode,
				detail = exception.Message,
				errors = exception is RequestValidationException validationException
					? validationException.Errors
					: null
			};

			context.Response.ContentType = "application/problem+json";
			context.Response.StatusCode = (int)statusCode;

			await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			})); 
		}

		private (HttpStatusCode StatusCode, string Title) MapException(Exception exception)
		{
			switch (exception)
			{
				case RequestValidationException:
					return (HttpStatusCode.BadRequest, "One or more validation errors occurred.");

				case NotFoundException:
					return (HttpStatusCode.NotFound, "The requested resource was not found.");

				case DomainException domainException:
					_logger.LogWarning(domainException, "Doain rule violation");
					return (HttpStatusCode.Conflict, "The request conflicts with a business rule.");

				default:
					// Truly unexpected — already logged in more detail by
					// UnhandledExceptionBehavior upstream, but log again here
					// since this is the last point before the response leaves
					// the API entirely.
					_logger.LogError(exception, "Unhandled exception");
					return (HttpStatusCode.InternalServerError, "An unexpected error occurred."); 
			}
		}
	}
}
