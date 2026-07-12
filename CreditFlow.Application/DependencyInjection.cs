using CreditFlow.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CreditFlow.Application
{
	/// <summary>
	/// Registers everything the Application layer owns: MediatR handlers,
	/// FluentValidation validators, and the pipeline behaviors that wrap every
	/// command/query. Kept as an extension method on IServiceCollection so
	/// CreditFlow.Api's Program.cs can wire up this whole layer with a single
	/// call, without knowing what's inside it.
	/// </summary>
	public static class DependencyInjection
	{
		public static IServiceCollection AddApplication(this IServiceCollection services)
		{
			services.AddMediatR(cfg =>
				cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

			services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

			// Order matters: this is the order behaviors wrap around the
			// eventual handler. UnhandledExceptionBehavior is outermost (logs
			// anything that escapes), LoggingBehavior wraps the timed
			// execution, ValidationBehavior runs last, right before the
			// handler — no point logging "handling X" if it's about to fail
			// validation anyway... but we log first so failed validation
			// attempts are still visible in the logs.
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

			return services; 

		}
	}
}
